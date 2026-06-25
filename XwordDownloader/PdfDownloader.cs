using Azure;
using Azure.Storage.Files.Shares;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Drive.v3;
using Google.Apis.Services;
using Google.Apis.Upload;
using System.Text;

namespace XwordDownloader;

public class PdfDownloader
{
    private const string DownloadToAzureFileStorageSettingName = "DownloadToAzureFileStorage";
    private const string GoogleApiSecretsJsonSettingName = "GoogleApiSecretsJson";

    /// <summary>
    /// Download the specified response stream to the configured puzzle destinations.
    /// </summary>
    public static async Task DownloadPdf(string filename, HttpResponseMessage response)
    {
        #if DEBUG
        if (IsGoogleDriveConfigured())
        {
            await DownloadToGoogleDrive(filename, response);
        }
        await DownloadLocally(filename, response);
        #else
        await DownloadToGoogleDrive(filename, response);
        if (IsAzureFileStorageDownloadEnabled())
        {
            await DownloadToAzureStorage(filename, response);
        }
        #endif
    }

    private static bool IsGoogleDriveConfigured()
    {
        return !string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("GoogleDriveFolderId")) &&
               (!string.IsNullOrWhiteSpace(GetGoogleApiSecretsJson()) ||
                IsGoogleApiSecretsFileStorageConfigured());
    }

    private static bool IsGoogleApiSecretsFileStorageConfigured()
    {
        return !string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("AzureWebJobsStorage")) &&
               !string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("GoogleApiSecretsFileName"));
    }

    private static bool IsAzureFileStorageDownloadEnabled()
    {
        var setting = Environment.GetEnvironmentVariable(DownloadToAzureFileStorageSettingName);
        return setting?.Equals("true", StringComparison.OrdinalIgnoreCase) == true ||
               setting?.Equals("1", StringComparison.OrdinalIgnoreCase) == true ||
               setting?.Equals("yes", StringComparison.OrdinalIgnoreCase) == true;
    }

    private static async Task DownloadToAzureStorage(string filename, HttpResponseMessage response)
    {
        response.EnsureSuccessStatusCode();

        var contentDisposition = response.Content.Headers.ContentDisposition;
        var finalFilename = contentDisposition?.FileName ?? filename;
        var bytes = await response.Content.ReadAsByteArrayAsync();

        var connectionString = Environment.GetEnvironmentVariable("AzureWebJobsStorage") ??
                               throw new Exception("AzureWebJobsStorage not found");
        var shareClient = new ShareClient(connectionString, "puzzles");
        await shareClient.CreateIfNotExistsAsync();

        var directoryClient = shareClient.GetRootDirectoryClient();
        var fileClient = directoryClient.GetFileClient(finalFilename);

        using var stream = new MemoryStream(bytes);
        await fileClient.CreateAsync(stream.Length);
        await fileClient.UploadRangeAsync(new HttpRange(0, stream.Length), stream);
    }
    
    private static async Task DownloadToGoogleDrive(string filename, HttpResponseMessage response)
    {
        // This uses a service account file to upload the file to Google Drive
        response.EnsureSuccessStatusCode();

        var contentDisposition = response.Content.Headers.ContentDisposition;
        var finalFilename = contentDisposition?.FileName ?? filename;
        var bytes = await response.Content.ReadAsByteArrayAsync();
        var secretsFile = await GetSecretsFile();
        var scopes = new[] { DriveService.ScopeConstants.DriveFile };
        var credential = (await CredentialFactory
            .FromStreamAsync<ServiceAccountCredential>(new MemoryStream(secretsFile), CancellationToken.None))
            .ToGoogleCredential();
        var scopedCredential = credential.CreateScoped(scopes);
            
        var service = new DriveService(new BaseClientService.Initializer()
        {
            HttpClientInitializer = scopedCredential,
            ApplicationName = "xword-downloader"
        });
        var parentFolderId = Environment.GetEnvironmentVariable("GoogleDriveFolderId");
        if (parentFolderId == null)
        {
            throw new Exception("GoogleDriveFolderId not found in environment variables.");
        }
        var fileMetadata = new Google.Apis.Drive.v3.Data.File()
        {
            Name = finalFilename,
            Parents = new List<string> { parentFolderId }
        };

        if (await GoogleDriveFileExists(service, parentFolderId, finalFilename))
        {
            return;
        }

        using var stream = new MemoryStream(bytes);
        var request = service.Files.Create(fileMetadata, stream, "application/pdf");
        request.Fields = "id";
        var uploadResponse = await request.UploadAsync();
        if (uploadResponse.Status != UploadStatus.Completed)
        {
            throw new Exception($"Upload failed for {filename}");
        }
    }

    private static async Task<bool> GoogleDriveFileExists(DriveService service, string parentFolderId, string filename)
    {
        var request = service.Files.List();
        request.Q = $"'{EscapeGoogleDriveQueryValue(parentFolderId)}' in parents and name = '{EscapeGoogleDriveQueryValue(filename)}' and trashed = false";
        request.Fields = "files(id)";
        request.PageSize = 1;

        var response = await request.ExecuteAsync();
        return response.Files.Count > 0;
    }

    private static string EscapeGoogleDriveQueryValue(string value)
    {
        return value.Replace("\\", "\\\\").Replace("'", "\\'");
    }

    private static async Task<byte[]> GetSecretsFile()
    {
        var googleApiSecretsJson = GetGoogleApiSecretsJson();
        if (!string.IsNullOrWhiteSpace(googleApiSecretsJson))
        {
            return Encoding.UTF8.GetBytes(googleApiSecretsJson);
        }

        return await GetSecretsFileFromAzureFileStorage();
    }

    private static string? GetGoogleApiSecretsJson()
    {
        var setting = Environment.GetEnvironmentVariable(GoogleApiSecretsJsonSettingName);
        if (string.IsNullOrWhiteSpace(setting))
        {
            return null;
        }

        var trimmed = setting.TrimStart();
        return trimmed.StartsWith("{", StringComparison.Ordinal) ? setting : null;
    }

    private static async Task<byte[]> GetSecretsFileFromAzureFileStorage()
    {
        var connectionString = Environment.GetEnvironmentVariable("AzureWebJobsStorage") ??
                               throw new Exception("AzureWebJobsStorage not found");
        var secretsFilename = Environment.GetEnvironmentVariable("GoogleApiSecretsFileName");
        var shareClient = new ShareClient(connectionString, "secrets");
        var directoryClient = shareClient.GetRootDirectoryClient();
        var fileClient = directoryClient.GetFileClient(secretsFilename);
        var download = (await fileClient.DownloadAsync()).Value;
        var stream = new MemoryStream();
        await download.Content.CopyToAsync(stream);
        return stream.ToArray();
    }

    private static async Task DownloadLocally(string filename, HttpResponseMessage response)
    {
        
        response.EnsureSuccessStatusCode();

        var contentDisposition = response.Content.Headers.ContentDisposition;
        var fileName = contentDisposition?.FileName ?? filename;
        var bytes = await response.Content.ReadAsByteArrayAsync();
        var filePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads", fileName);
        if (File.Exists(filePath))
        {
            File.Delete(filePath);
        }
        await File.WriteAllBytesAsync(filePath, bytes);
    }
}

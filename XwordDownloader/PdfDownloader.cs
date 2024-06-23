using Azure;
using Azure.Storage.Files.Shares;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Drive.v3;
using Google.Apis.Services;
using Google.Apis.Upload;

namespace XwordDownloader;

public class PdfDownloader
{
    /// <summary>
    /// Download the specified response stream to a file in Azure Storage.
    ///
    /// The file will be stored in a folder called puzzles which is created if it doesn't exist.
    /// </summary>
    public static async Task DownloadPdf(string filename, HttpResponseMessage response)
    {
        #if DEBUG
        await DownloadToGoogleDrive(filename, response);
        await DownloadLocally(filename, response);
        return;
        #endif
        await DownloadToGoogleDrive(filename, response);
        await DownloadToAzureStorage(filename, response);
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
        var credential = await GoogleCredential.FromStreamAsync(new MemoryStream(secretsFile), CancellationToken.None);
        if (credential == null)
        {
            throw new Exception("Failed to create credentials");
        }
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

        using var stream = new MemoryStream(bytes);
        var request = service.Files.Create(fileMetadata, stream, "application/pdf");
        request.Fields = "id";
        var uploadResponse = await request.UploadAsync();
        if (uploadResponse.Status != UploadStatus.Completed)
        {
            throw new Exception($"Upload failed for {filename}");
        }
    }

    private static async Task<byte[]> GetSecretsFile()
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
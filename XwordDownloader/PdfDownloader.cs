using Azure;
using Azure.Storage.Files.Shares;

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
        await DownloadLocally(filename, response);
        return;
        #endif
        response.EnsureSuccessStatusCode();

        var contentDisposition = response.Content.Headers.ContentDisposition;
        var fileName = contentDisposition?.FileName ?? filename;
        var bytes = await response.Content.ReadAsByteArrayAsync();

        var connectionString = Environment.GetEnvironmentVariable("AzureWebJobsStorage") ??
                               throw new Exception("AzureWebJobsStorage not found");
        var shareClient = new ShareClient(connectionString, "puzzles");
        await shareClient.CreateIfNotExistsAsync();

        var directoryClient = shareClient.GetRootDirectoryClient();
        var fileClient = directoryClient.GetFileClient(fileName);

        using var stream = new MemoryStream(bytes);
        await fileClient.CreateAsync(stream.Length);
        await fileClient.UploadRangeAsync(new HttpRange(0, stream.Length), stream);
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
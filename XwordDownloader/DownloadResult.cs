namespace XwordDownloader;

public record DownloadResult(DownloadAttemptStatus Status, string Message)
{
    public static DownloadResult Succeeded(string message) => new(DownloadAttemptStatus.Succeeded, message);
    public static DownloadResult Skipped(string message) => new(DownloadAttemptStatus.Skipped, message);
}

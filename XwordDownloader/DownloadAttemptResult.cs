namespace XwordDownloader;

public record DownloadAttemptResult(
    string Source,
    DateTimeOffset StartedAt,
    DateTimeOffset FinishedAt,
    DownloadAttemptStatus Status,
    string? Message,
    string? Error)
{
    public TimeSpan Duration => FinishedAt - StartedAt;
    public bool Succeeded => Status == DownloadAttemptStatus.Succeeded;
    public bool Failed => Status == DownloadAttemptStatus.Failed;
}

public enum DownloadAttemptStatus
{
    Succeeded,
    Skipped,
    Failed
}

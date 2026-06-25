using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace XwordDownloader
{
    public class Downloader
    {
        private readonly ILogger _logger;
        private readonly RunNotificationService _notificationService;

        public Downloader(ILoggerFactory loggerFactory)
        {
            _logger = loggerFactory.CreateLogger<Downloader>();
            _notificationService = new RunNotificationService(_logger);
        }

        [Function("Downloader")]
        public async Task Run([TimerTrigger("0 8 * * *")] TimerInfo myTimer)
        {
            var startedAt = DateTimeOffset.UtcNow;
            _logger.LogInformation("C# Timer trigger function executed at: {StartedAt}", startedAt);

            var attempts = new List<DownloadAttemptResult>
            {
                await RunDownload("New York Times", () => new NewYorkTimes().DownloadPuzzle()),
                await RunDownload("Washington Post Sunday", () => new WaPoSunday().DownloadPuzzle()),
                await RunDownload("Wall Street Journal", () => new WallStreetJournalContest().DownloadPuzzle())
            };

            var finishedAt = DateTimeOffset.UtcNow;

            if (myTimer.ScheduleStatus is not null)
            {
                _logger.LogInformation("Next timer schedule at: {NextRun}", myTimer.ScheduleStatus.Next);
            }

            try
            {
                await _notificationService.SendRunSummary(
                    attempts,
                    startedAt,
                    finishedAt,
                    myTimer.ScheduleStatus?.Next);
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, "Failed to send downloader run notification.");
            }
        }

        private async Task<DownloadAttemptResult> RunDownload(
            string source,
            Func<Task<DownloadResult>> download)
        {
            var startedAt = DateTimeOffset.UtcNow;
            try
            {
                var result = await download();
                var finishedAt = DateTimeOffset.UtcNow;
                _logger.LogInformation(
                    "{Source} finished with status {Status}: {Message}",
                    source,
                    result.Status,
                    result.Message);

                return new DownloadAttemptResult(
                    source,
                    startedAt,
                    finishedAt,
                    result.Status,
                    result.Message,
                    null);
            }
            catch (Exception exception)
            {
                var finishedAt = DateTimeOffset.UtcNow;
                _logger.LogError(exception, "{Source} failed.", source);
                return new DownloadAttemptResult(
                    source,
                    startedAt,
                    finishedAt,
                    DownloadAttemptStatus.Failed,
                    null,
                    exception.ToString());
            }
        }
    }
}

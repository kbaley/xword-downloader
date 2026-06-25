using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace XwordDownloader
{
    public class Downloader
    {
        private readonly ILogger _logger;

        public Downloader(ILoggerFactory loggerFactory)
        {
            _logger = loggerFactory.CreateLogger<Downloader>();
        }

        [Function("Downloader")]
        public async Task Run([TimerTrigger("0 8 * * *")] TimerInfo myTimer)
        {
            _logger.LogInformation($"C# Timer trigger function executed at: {DateTime.Now}");
            await TryDownload("New York Times", () => new NewYorkTimes().DownloadPuzzle());
            await TryDownload("Washington Post Sunday", () => new WaPoSunday().DownloadPuzzle());
            await TryDownload("Wall Street Journal", () => new WallStreetJournalContest().DownloadPuzzle());

            if (myTimer.ScheduleStatus is not null)
            {
                _logger.LogInformation($"Next timer schedule at: {myTimer.ScheduleStatus.Next}");
            }
        }

        private async Task TryDownload(string source, Func<Task> download)
        {
            try
            {
                await download();
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, "{Source} download failed.", source);
            }
        }
    }
}

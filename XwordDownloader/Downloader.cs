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
            await new NewYorkTimes().DownloadPuzzle();
            await new WaPoSunday().DownloadPuzzle();
            await new WallStreetJournalContest().DownloadPuzzle();

            if (myTimer.ScheduleStatus is not null)
            {
                _logger.LogInformation($"Next timer schedule at: {myTimer.ScheduleStatus.Next}");
            }
        }
    }
}

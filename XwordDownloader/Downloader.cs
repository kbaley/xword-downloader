using System.Text.Json;
using Azure;
using Azure.Storage.Blobs;
using Azure.Storage.Files.Shares;
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
            var nytsCookie = Environment.GetEnvironmentVariable("NYTS_COOKIE") ?? "";
            var cookies = "NYT-S=" + nytsCookie;
            const int numdays = 1;
            _logger.LogInformation($"C# Timer trigger function executed at: {DateTime.Now}");

            using (var httpClient = new HttpClient())
            {
                httpClient.DefaultRequestHeaders.Add("Cookie", cookies);

                var requestUrl = $"https://www.nytimes.com/svc/crosswords/v3/36569100/puzzles.json?publish_type=daily&sort_order=asc&sort_by=print_date&limit={numdays}";
                var response = await httpClient.GetAsync(requestUrl);
                response.EnsureSuccessStatusCode();

                var jsonResponse = await response.Content.ReadAsStringAsync();
                var puzzleIds = ParsePuzzleIds(jsonResponse);

                foreach (var puzzleId in puzzleIds)
                {
                    var puzzleUrl = $"https://www.nytimes.com/svc/crosswords/v2/puzzle/{puzzleId}.pdf?southpaw=true";
                    Console.WriteLine(puzzleUrl);

                    await DownloadPdf(httpClient, cookies, puzzleUrl, puzzleId);
                }
            }
            
            if (myTimer.ScheduleStatus is not null)
            {
                _logger.LogInformation($"Next timer schedule at: {myTimer.ScheduleStatus.Next}");
            }
        }

        static async Task DownloadPdf(HttpClient httpClient, string cookies, string url, int puzzleId)
        {
            var request = new HttpRequestMessage(HttpMethod.Get, url);
            request.Headers.Add("Cookie", cookies);

            var response = await httpClient.SendAsync(request);
            response.EnsureSuccessStatusCode();

            var contentDisposition = response.Content.Headers.ContentDisposition;
            var fileName = contentDisposition?.FileName ?? $"{puzzleId}.pdf";
            var bytes = await response.Content.ReadAsByteArrayAsync();

            var connectionString = Environment.GetEnvironmentVariable("AzureWebJobsStorage") ?? throw new Exception("AzureWebJobsStorage not found");
            var shareClient = new ShareClient(connectionString, "puzzles");
            await shareClient.CreateIfNotExistsAsync();

            var directoryClient = shareClient.GetRootDirectoryClient();
            var fileClient = directoryClient.GetFileClient(fileName);

            using var stream = new MemoryStream(bytes);
            await fileClient.CreateAsync(stream.Length);
            await fileClient.UploadRangeAsync(new HttpRange(0, stream.Length), stream);
        }


        static List<int> ParsePuzzleIds(string jsonResponse)
        {
            var puzzleIds = new List<int>();
            var jsonDoc = JsonDocument.Parse(jsonResponse);
            foreach (var result in jsonDoc.RootElement.GetProperty("results").EnumerateArray())
            {
                puzzleIds.Add(result.GetProperty("puzzle_id").GetInt32());
            }
            return puzzleIds;
        }
    }
}

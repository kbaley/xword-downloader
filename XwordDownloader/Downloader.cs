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
            _logger.LogInformation($"C# Timer trigger function executed at: {DateTime.Now}");
            // await DownloadNytPuzzle();
            await DownloadWapoSundayPuzzle();

            if (myTimer.ScheduleStatus is not null)
            {
                _logger.LogInformation($"Next timer schedule at: {myTimer.ScheduleStatus.Next}");
            }
        }

        private async Task DownloadWapoSundayPuzzle()
        {
            const string url = "https://cdn1.amuselabs.com/wapo/crossword-pdf";
            var parms = new Dictionary<string, string>();
            parms.Add("id", "ebirnholz_240609");
            parms.Add("set", "wapo-eb");
            parms.Add("theme", "wapo");
            parms.Add("locale", "en-US");
            parms.Add("print", "1");
            parms.Add("checkPDF", "true");
            using var httpClient = new HttpClient();
            using var request = new HttpRequestMessage(HttpMethod.Post, url)
            {
                Content = new FormUrlEncodedContent(parms)
            };
            using var response = await httpClient.SendAsync(request);
            response.EnsureSuccessStatusCode();
            
            await DownloadLocalPdf("ebirnholz_240609.pdf", response);
        }

        private async Task DownloadNytPuzzle()
        {
            var nytsCookie = Environment.GetEnvironmentVariable("NYTS_COOKIE") ?? "";
            var cookies = "NYT-S=" + nytsCookie;
            const int numdays = 1;

            using var httpClient = new HttpClient();
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

        static async Task DownloadPdf(HttpClient httpClient, string cookies, string url, int puzzleId)
        {
            var request = new HttpRequestMessage(HttpMethod.Get, url);
            request.Headers.Add("Cookie", cookies);

            var response = await httpClient.SendAsync(request);
            await DownloadLocalPdf($"{puzzleId}.pdf", response);
        }

        private static async Task DownloadLocalPdf(string filename, HttpResponseMessage response)
        {
            response.EnsureSuccessStatusCode();

            var contentDisposition = response.Content.Headers.ContentDisposition;
            var fileName = contentDisposition?.FileName ?? filename;
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

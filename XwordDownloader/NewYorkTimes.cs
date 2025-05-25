using System.Text.Json;

namespace XwordDownloader;

public class NewYorkTimes
{
    /// <summary>
    /// Download the New York Times crossword puzzles for the last specified number of days.
    ///
    /// This requires a NYTS_COOKIE environment variable to be set with the NYT-S cookie value.
    /// I.e. you need a subscription
    /// </summary>
    public async Task DownloadPuzzle(int numberOfDays = 1)
    {
        var nytsCookie = Environment.GetEnvironmentVariable("NYTS_COOKIE") ?? "";
        if (string.IsNullOrEmpty(nytsCookie))
        {
            throw new Exception("NYTS_COOKIE environment variable not found.");
        }
        var cookies = "NYT-S=" + nytsCookie;

        using var httpClient = new HttpClient();
        httpClient.DefaultRequestHeaders.Add("Cookie", cookies);

        var requestUrl = $"https://www.nytimes.com/svc/crosswords/v3/36569100/puzzles.json?publish_type=daily&sort_order=asc&sort_by=print_date&limit={numberOfDays}";
        var response = await httpClient.GetAsync(requestUrl);
        response.EnsureSuccessStatusCode();

        var jsonResponse = await response.Content.ReadAsStringAsync();
        var puzzleIds = ParsePuzzleIds(jsonResponse);

        foreach (var puzzleId in puzzleIds)
        {
            // Default to left-handed. If you're right-handed, I'll take a pull request but I don't expect
            // anyone else to use this but me so it's hard-coded
            // For Sunday, print in large print
            var printOption = "southpaw=true";
            if (puzzleId.IsSunday)
            {
                printOption = "large_print=true";
            }
            var puzzleUrl = $"https://www.nytimes.com/svc/crosswords/v2/puzzle/{puzzleId.PuzzleId}.pdf?{printOption}";
            Console.WriteLine(puzzleUrl);

            var request = new HttpRequestMessage(HttpMethod.Get, puzzleUrl);
            request.Headers.Add("Cookie", cookies);

            response = await httpClient.SendAsync(request);
            await PdfDownloader.DownloadPdf($"{puzzleId}.pdf", response);
        }
    }

    private static List<(int PuzzleId, bool IsSunday)> ParsePuzzleIds(string jsonResponse)
    {
        var puzzleIds = new List<(int, bool)>();
        var jsonDoc = JsonDocument.Parse(jsonResponse);
        foreach (var result in jsonDoc.RootElement.GetProperty("results").EnumerateArray())
        {
            var puzzleId = result.GetProperty("puzzle_id").GetInt32();
            var printDate = result.GetProperty("print_date").GetDateTime();
            var isSunday = printDate.DayOfWeek == DayOfWeek.Sunday;
            puzzleIds.Add((puzzleId, isSunday));
        }
        return puzzleIds;
    }
}
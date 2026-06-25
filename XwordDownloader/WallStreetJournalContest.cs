namespace XwordDownloader;

using System.Net.Http.Headers;
using System.Text.RegularExpressions;

/// <summary>
/// The Wall Street Journal daily crossword puzzle
/// </summary>
public class WallStreetJournalContest
{
    private const string PuzzleIndexUrl = "https://www.wsj.com/news/puzzle";
    private const string PdfBaseUrl = "https://prod-i.a.dj.com/public/resources/documents/";
    private static readonly Regex CrosswordArticleRegex = new(
        """https://www\.wsj\.com/articles/(?<slug>[^"<>\\]*?-crossword-[^"<>\\]*)""",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    public async Task<DownloadResult> DownloadPuzzle()
    {
        using var httpClient = new HttpClient(new HttpClientHandler { UseCookies = false });
        ConfigureBrowserHeaders(httpClient);

        var articleSlug = await GetLatestCrosswordSlug(httpClient);
        var pdfUrl = await FindPuzzlePdfUrl(httpClient, articleSlug);
        var filename = $"WSJ-{DateTime.Now:yyyy-MM-dd}.pdf";
        using var request = new HttpRequestMessage(HttpMethod.Get, pdfUrl);
        using var response = await httpClient.SendAsync(request);

        await PdfDownloader.DownloadPdf(filename, response);
        return DownloadResult.Succeeded($"Downloaded WSJ puzzle from {pdfUrl}.");
    }

    private static void ConfigureBrowserHeaders(HttpClient httpClient)
    {
        httpClient.DefaultRequestHeaders.UserAgent.ParseAdd(
            "Mozilla/5.0 (Macintosh; Intel Mac OS X 10_15_7) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/125.0.0.0 Safari/537.36");
        httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("text/html"));
        httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/xhtml+xml"));
        httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/xml", 0.9));
        httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("*/*", 0.8));
        httpClient.DefaultRequestHeaders.AcceptLanguage.Add(new StringWithQualityHeaderValue("en-US"));
        httpClient.DefaultRequestHeaders.AcceptLanguage.Add(new StringWithQualityHeaderValue("en", 0.9));
    }

    private static async Task<string> GetLatestCrosswordSlug(HttpClient httpClient)
    {
        var html = await httpClient.GetStringAsync(PuzzleIndexUrl);
        var match = CrosswordArticleRegex.Match(html);
        if (!match.Success)
        {
            throw new Exception("Could not find a Wall Street Journal crossword article on the puzzle index.");
        }

        return match.Groups["slug"].Value;
    }

    private static async Task<string> FindPuzzlePdfUrl(HttpClient httpClient, string articleSlug)
    {
        foreach (var candidate in GetPdfFilenameCandidates(articleSlug))
        {
            var url = $"{PdfBaseUrl}{candidate}.pdf";
            using var request = new HttpRequestMessage(HttpMethod.Head, url);
            using var response = await httpClient.SendAsync(request);
            if (response.IsSuccessStatusCode &&
                response.Content.Headers.ContentType?.MediaType == "application/pdf")
            {
                return url;
            }
        }

        throw new Exception($"Could not find a Wall Street Journal crossword PDF for article slug '{articleSlug}'.");
    }

    private static IEnumerable<string> GetPdfFilenameCandidates(string articleSlug)
    {
        var titleSlug = Regex.Replace(
            articleSlug,
            "-(?:monday|tuesday|wednesday|thursday|friday|saturday|sunday)-crossword-.*$",
            "",
            RegexOptions.IgnoreCase);
        var words = titleSlug
            .Split('-', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(word => Regex.Replace(word, "[^a-z0-9]", "", RegexOptions.IgnoreCase).ToLowerInvariant())
            .Where(word => word.Length > 0)
            .ToArray();

        foreach (var candidate in BuildFilenameCandidates(words))
        {
            yield return candidate;
        }
    }

    private static IEnumerable<string> BuildFilenameCandidates(string[] words)
    {
        var seen = new HashSet<string>(StringComparer.Ordinal);
        foreach (var candidateWords in GetCandidateWordSets(words))
        {
            var joined = string.Join("", candidateWords);
            if (joined.Length == 0)
            {
                continue;
            }

            var filename = char.ToUpperInvariant(joined[0]) + joined[1..];
            if (seen.Add(filename))
            {
                yield return filename;
            }
        }
    }

    private static IEnumerable<string[]> GetCandidateWordSets(string[] words)
    {
        yield return words;

        var throwawayFirstWords = new HashSet<string> { "a", "an", "the", "my", "your", "his", "her", "its", "our", "their" };
        if (words.Length > 1 && throwawayFirstWords.Contains(words[0]))
        {
            yield return words[1..];
        }
    }
}

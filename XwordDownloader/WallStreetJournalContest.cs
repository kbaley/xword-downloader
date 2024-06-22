namespace XwordDownloader;

/// <summary>
/// The Wall Street Journal Friday contest puzzle
/// </summary>
public class WallStreetJournalContest
{
    
    public async Task DownloadPuzzle()
    {
        if (DateTime.Now.DayOfWeek != DayOfWeek.Friday)
        {
            return;
        }

        
        var puzzleId = $"XWD{DateTime.Now:MMddyyyy}.pdf";
        puzzleId = "XWD06212024.pdf";
        var url = $"https://prod-i.a.dj.com/public/resources/documents/{puzzleId}";
        using var httpClient = new HttpClient();
        using var request = new HttpRequestMessage(HttpMethod.Get, url);
        using var response = await httpClient.SendAsync(request);

        await PdfDownloader.DownloadPdf(puzzleId, response);
    }
}
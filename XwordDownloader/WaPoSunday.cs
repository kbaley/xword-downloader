namespace XwordDownloader;

public class WaPoSunday
{

    public async Task DownloadPuzzle()
    {
        if (DateTime.Now.DayOfWeek != DayOfWeek.Sunday)
        {
            return;
        }

        const string url = "https://cdn1.amuselabs.com/wapo/crossword-pdf";
        var parms = new Dictionary<string, string>();
        var date = DateTime.Now.ToString("yyMMdd");
        parms.Add("id", $"ebirnholz_{date}");
        parms.Add("set", "wapo-eb");
        parms.Add("theme", "wapo");
        parms.Add("locale", "en-US");
        parms.Add("print", "1");
        parms.Add("checkPDF", "true");
        using var httpClient = new HttpClient();
        using var request = new HttpRequestMessage(HttpMethod.Post, url);
        request.Content = new FormUrlEncodedContent(parms);
        using var response = await httpClient.SendAsync(request);

        await PdfDownloader.DownloadPdf($"ebirnholz_{date}.pdf", response);
    }
}
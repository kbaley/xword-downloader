namespace XwordDownloader;

public class WaPoSunday
{

    public async Task DownloadPuzzle()
    {
        if (DateTime.Now.DayOfWeek != DayOfWeek.Sunday)
        {
            return;
        }

        var date = DateTime.Today.ToString("yyMMdd");
        // July 2025: WaPo changed to a different app and as of today, the print view is not good
        // Pulling from https://crosswordfiend.com/download/ for now
        var url = $"https://herbach.dnsalias.com/WaPo/wp{date}.pdf";
        using var httpClient = new HttpClient();
        using var request = new HttpRequestMessage(HttpMethod.Get, url);
        using var response = await httpClient.SendAsync(request);

        await PdfDownloader.DownloadPdf($"ebirnholz_{date}.pdf", response);
    }
}
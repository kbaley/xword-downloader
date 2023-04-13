using System;
using Microsoft.Playwright;
using System.Threading.Tasks;

namespace WSJDownloader
{
    class Program
    {
        static async Task Main(string[] args)
        {
            var downloadDir = System.IO.Directory.GetCurrentDirectory();
            var startDate = DateTime.Today.AddDays(-14);
            if (args.Length > 0) {
                downloadDir = args[0];
            }
            if (args.Length > 1) {
                startDate = DateTime.Parse(args[1]);
            }

            var url = "https://www.wsj.com/news/types/crossword-contest";
            using var playwright = await Playwright.CreateAsync();
            var browserOptions = new BrowserTypeLaunchOptions { Headless = true, DownloadsPath = downloadDir };
            await using var browser = await playwright.Chromium.LaunchAsync(browserOptions);
            var browserContextOptions = new BrowserNewContextOptions { AcceptDownloads = true };
            var context = await browser.NewContextAsync();
            var page = await context.NewPageAsync();
            await page.GotoAsync(url);

            startDate = startDate.AddDays(((int)DayOfWeek.Friday - (int)startDate.DayOfWeek + 7) % 7);
            while (startDate <= DateTime.Today) {
                Console.WriteLine($"Extracting puzzle for {startDate.ToString("MMMM d, yyyy")}");
                await ExtractPuzzle(page, startDate, context, downloadDir);
                startDate = startDate.AddDays(7);
            }
            Console.WriteLine("Done WSJ puzzle extraction");
        }
        
        private static async Task ExtractPuzzle(IPage page, DateTime date, IBrowserContext context, string downloadDir) {
            var dateText = $"Friday Crossword, {date:MMMM d}";
            await page.Locator($"a:has-text('{dateText}')").ClickAsync();
            await page.WaitForSelectorAsync("a:has-text('Download PDF')");
            var downloadTask = page.WaitForDownloadAsync();
            await page.GetByText("Download PDF.").ClickAsync();
            await page.Context.WaitForPageAsync();
            var download = await downloadTask;
            var path = System.IO.Path.Combine(downloadDir, $"WSJ-{date:yyyy-MM-dd}.pdf");
            await download.SaveAsAsync(path);
            await page.GoBackAsync();
        }
    }
}

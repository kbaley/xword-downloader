using System;
using System.Threading;
using OpenQA.Selenium;
using OpenQA.Selenium.Firefox;
using OpenQA.Selenium.Support.UI;

namespace WSJDownloader
{
    class Program
    {
        static void Main(string[] args)
        {
            var downloadDir = System.IO.Directory.GetCurrentDirectory();
            var startDate = DateTime.Today.AddDays(-14);
            if (args.Length > 0) {
                downloadDir = args[0];
            }
            if (args.Length > 1) {
                startDate = DateTime.Parse(args[1]);
            }
            var options = new FirefoxOptions();
            options.SetPreference("pdfjs.disabled", true);
            options.SetPreference("browser.download.folderList", 2);
            options.SetPreference("browser.download.dir", downloadDir);
            options.SetPreference("browser.download.useDownloadDir", true);
            options.SetPreference("browser.download.viewableInternally.enabledTypes", "");
            options.SetPreference("browser.helperApps.neverAsk.saveToDisk", "application/pdf;text/plain;application/text;text/xml;application/xml");

            var driver = new FirefoxDriver(options);
            driver.Url = "https://www.wsj.com/news/types/crossword-contest";
            startDate = startDate.AddDays(((int)DayOfWeek.Friday - (int)startDate.DayOfWeek + 7) % 7);
            while (startDate <= DateTime.Today) {
                Console.WriteLine($"Extracting puzzle for {startDate.ToString("MMMM d, yyyy")}");
                ExtractPuzzle(driver, startDate);
                startDate = startDate.AddDays(7);
            }
            Console.WriteLine("Done WSJ puzzle extraction");
            driver.Quit();
        }

        private static void ExtractPuzzle(IWebDriver driver, DateTime date) {
            var dateText = $"Friday Crossword, {date.ToString("MMMM d")}";
            var wait = new WebDriverWait(driver, TimeSpan.FromSeconds(10));
            var element = wait.Until(e => e.FindElement(By.PartialLinkText(dateText)));
            if (element == null) {
                Console.WriteLine($"Could not find crossword link");
                return;
            }
            ((IJavaScriptExecutor) driver).ExecuteScript("arguments[0].scrollIntoView(true);", element);
            Thread.Sleep(500);
            element.Click();
            element = driver.FindElement(By.LinkText("Download PDF"));
            if (element == null) {
                Console.WriteLine($"Could not find download link");
                return;
            }
            element.Click();
            driver.Navigate().Back();
            Thread.Sleep(1000);
        }
    }
}

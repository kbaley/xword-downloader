using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using XwordDownloader;

#if DEBUG

await new NewYorkTimes().DownloadPuzzle();
await new WaPoSunday().DownloadPuzzle();
await new WallStreetJournalContest().DownloadPuzzle();

#else

var host = new HostBuilder()
    .ConfigureFunctionsWebApplication()
    .ConfigureServices(services => {
        services.AddApplicationInsightsTelemetryWorkerService();
        services.ConfigureFunctionsApplicationInsights();
    })
    .Build();

host.Run();

#endif

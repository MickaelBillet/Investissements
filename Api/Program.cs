using InvestissementsDashboard.Api.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var host = new HostBuilder()
    .ConfigureFunctionsWebApplication()
    .ConfigureServices(services =>
    {
        services.AddHttpClient<IGoogleSheetsService, GoogleSheetsService>();
        services.AddScoped<ISnapshotService, SnapshotService>();
        services.AddScoped<IAssetsService, AssetsService>();
    })
    .Build();

host.Run();

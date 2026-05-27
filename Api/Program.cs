using InvestissementsDashboard.Api.Services.Mcp;
using InvestissementsDashboard.Api.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var host = new HostBuilder()
    .ConfigureFunctionsWebApplication()
    .ConfigureServices(services =>
    {
        services.AddHttpClient<IAppsScriptService, AppsScriptService>();
        services.AddScoped<IAssetsService, AssetsService>();
        services.AddScoped<ISnapshotService, SnapshotService>();
        services.AddScoped<IPortfolioMetricsService, PortfolioMetricsService>();
        services.AddScoped<IGeographyService, GeographyService>();
        services.AddScoped<IMcpService, McpService>();
    })
    .Build();

host.Run();

using InvestissementsDashboard.Api.Middleware;
using InvestissementsDashboard.Api.Services.Mcp;
using InvestissementsDashboard.Api.Services;
using InvestissementsDashboard.GoogleSheets;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;

var host = new HostBuilder()
    .ConfigureFunctionsWebApplication(builder =>
    {
        builder.UseMiddleware<DashboardAuthMiddleware>();
    })
    .ConfigureServices(services =>
    {
        services.AddSingleton<IGoogleSheetsClient, GoogleSheetsClient>();
        services.AddMemoryCache();
        services.AddScoped<IAssetsService, AssetsService>();
        services.AddScoped<ISnapshotService, SnapshotService>();
        services.AddScoped<IPortfolioMetricsService, PortfolioMetricsService>();
        services.AddScoped<IGeographyService, GeographyService>();
        services.AddScoped<IBondScheduleService, BondScheduleService>();
        services.AddScoped<IMcpService, McpService>();
        services.AddHttpClient<ISyncService, SyncService>(c => c.Timeout = TimeSpan.FromSeconds(100));
    })
    .Build();

host.Run();

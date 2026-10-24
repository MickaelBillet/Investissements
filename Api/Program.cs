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
        services.AddSingleton(TimeProvider.System);
        services.AddScoped<IAssetsService, AssetsService>();
        services.AddScoped<ISnapshotService, SnapshotService>();
        services.AddScoped<IPortfolioMetricsService, PortfolioMetricsService>();
        services.AddScoped<IGeographyService, GeographyService>();
        services.AddScoped<ISectorService, SectorService>();
        services.AddScoped<IBondScheduleService, BondScheduleService>();
        services.AddScoped<IWatchlistService, WatchlistService>();
        services.AddScoped<IMcpService, McpService>();
        services.AddHttpClient<ISyncService, SyncService>(c => c.Timeout = TimeSpan.FromSeconds(100));
        // A search takes 1–3 s: 30 s stays well within the 45 s of the Static Web Apps proxy.
        services.AddHttpClient<IWebSearchService, WebSearchService>(c => c.Timeout = TimeSpan.FromSeconds(30));
    })
    .Build();

host.Run();

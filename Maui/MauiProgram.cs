using AgentAI;
using InvestissementsDashboard.Client.Extensions;
using InvestissementsDashboard.Client.Services;
using InvestissementsDashboard.Maui.Services;
using Microsoft.Extensions.Logging;

namespace InvestissementsDashboard.Maui;

public static class MauiProgram
{
    // Same origin as the deployed site: the Api is only reachable through the SWA proxy.
    private static readonly Uri ApiBaseUri = new("https://invest.zapto.fr/");

    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder.UseMauiApp<App>();

        builder.Services.AddMauiBlazorWebView();
        builder.Services.AddSingleton<ISecureStorage>(SecureStorage.Default);
        builder.Services.AddSingleton<IKeyValueStore, SecureStorageKeyValueStore>();
        builder.Services.AddInvestissementsClient(ApiBaseUri);
        builder.Services.AddSingleton<IAgentSettings>(_ => new PreferencesAgentSettings(Preferences.Default, ApiBaseUri.ToString()));
        AddAgents(builder.Services);

#if DEBUG
        builder.Services.AddBlazorWebViewDeveloperTools();
        builder.Logging.AddDebug();
#endif

        return builder.Build();
    }

    private static void AddAgents(IServiceCollection services)
    {
        services.AddSingleton(sp => new AgentOptionsProvider(
            sp.GetRequiredService<IAgentSettings>(),
            ApiBaseUri,
            // The install folder is read-only for a packaged app, so the history goes to the user's data folder.
            Path.Combine(FileSystem.AppDataDirectory, "agents-history")));

        // Dedicated clients: the dashboard's HttpClient adds the dashboard password header, which must not leak to RSS feeds.
        services.AddSingleton<INewsService>(sp => new RssNewsService(
            new HttpClient(),
            TimeProvider.System,
            sp.GetRequiredService<ILogger<RssNewsService>>()));
        services.AddSingleton(sp => new InvestZaptoMcpClient(
            new HttpClient(new ForceContentLengthHandler { InnerHandler = new SocketsHttpHandler() }),
            sp.GetRequiredService<ILoggerFactory>()));

        services.AddSingleton<IAgentRunner>(sp =>
        {
            var mcpClient = sp.GetRequiredService<InvestZaptoMcpClient>();
            var newsService = sp.GetRequiredService<INewsService>();
            var loggerFactory = sp.GetRequiredService<ILoggerFactory>();
            return new AgentRunner(
                sp.GetRequiredService<AgentOptionsProvider>(),
                options => new AgentFactory(options, mcpClient, newsService, loggerFactory),
                sp.GetRequiredService<ILogger<AgentRunner>>());
        });
    }
}

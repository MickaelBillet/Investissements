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
        AddLogger(builder.Services);


#if DEBUG
        builder.Services.AddBlazorWebViewDeveloperTools();
        builder.Logging.AddDebug();
#endif

        return builder.Build();
    }

    // These clients are captured by singletons (the agent factory, the news service), so their handler is never
    // recycled by the factory: bounding the connection lifetime keeps DNS changes picked up.
    private static HttpMessageHandler CreatePrimaryHandler() =>
        new SocketsHttpHandler { PooledConnectionLifetime = TimeSpan.FromMinutes(2) };

    private static void AddLogger(IServiceCollection services)
    {
        services.AddLogging(builder =>
        {
            builder.AddProvider(new FileLoggerProvider(Path.Combine(FileSystem.AppDataDirectory, "logs", "maui.log")));
            builder.AddFilter<FileLoggerProvider>("Microsoft", LogLevel.Warning);
            builder.AddFilter<FileLoggerProvider>("System", LogLevel.Warning);
        });
    }

    private static void AddAgents(IServiceCollection services)
    {
        services.AddSingleton(sp => new AgentOptionsProvider(
            sp.GetRequiredService<IAgentSettings>(),
            ApiBaseUri,
            // The install folder is read-only for a packaged app, so the history goes to the user's data folder.
            Path.Combine(FileSystem.AppDataDirectory, "agents-history")));

        // Typed clients: each has its own pipeline, so the dashboard password header added to the dashboard
        // clients never reaches the RSS feeds or the MCP server.
        services.AddNewsServices();
        services.AddHttpClient<IRssFeedReader, RssFeedReader>().ConfigurePrimaryHttpMessageHandler(CreatePrimaryHandler);
        services.AddHttpClient<YahooTickerResolver>().ConfigurePrimaryHttpMessageHandler(CreatePrimaryHandler);
        services.AddInvestZaptoMcp()
            .AddHttpMessageHandler(sp => new McpApiKeyHandler(sp.GetRequiredService<IKeyValueStore>()))
            .ConfigurePrimaryHttpMessageHandler(CreatePrimaryHandler);

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

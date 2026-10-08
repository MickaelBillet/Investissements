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
        builder.Services.AddSingleton<IAgentRunner, PlaceholderAgentRunner>();

#if DEBUG
        builder.Services.AddBlazorWebViewDeveloperTools();
        builder.Logging.AddDebug();
#endif

        return builder.Build();
    }
}

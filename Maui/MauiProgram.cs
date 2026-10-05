using InvestissementsDashboard.Client.Extensions;
using InvestissementsDashboard.Client.Services;
using InvestissementsDashboard.Maui.Services;
using Microsoft.Extensions.Logging;

namespace InvestissementsDashboard.Maui;

public static class MauiProgram
{
    // Same origin as the deployed site: the Api is only reachable through the SWA proxy.
    private const string DefaultApiBaseUrl = "https://invest.zapto.fr/";
    private const string ApiBaseUrlEnvironmentVariable = "INVEST_API_BASE_URL";

    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder.UseMauiApp<App>();

        builder.Services.AddMauiBlazorWebView();
        builder.Services.AddSingleton<ISecureStorage>(SecureStorage.Default);
        builder.Services.AddSingleton<IKeyValueStore, SecureStorageKeyValueStore>();
        builder.Services.AddInvestissementsClient(ResolveApiBaseUri());

#if DEBUG
        builder.Services.AddBlazorWebViewDeveloperTools();
        builder.Logging.AddDebug();
#endif

        return builder.Build();
    }

    private static Uri ResolveApiBaseUri()
    {
        var configured = Environment.GetEnvironmentVariable(ApiBaseUrlEnvironmentVariable);
        return new Uri(string.IsNullOrWhiteSpace(configured) ? DefaultApiBaseUrl : configured);
    }
}

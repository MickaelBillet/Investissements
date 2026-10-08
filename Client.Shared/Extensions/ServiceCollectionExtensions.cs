using System.Globalization;
using ApexCharts;
using InvestissementsDashboard.Client.Services;
using InvestissementsDashboard.Client.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using MudBlazor.Services;

namespace InvestissementsDashboard.Client.Extensions;

public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers everything the shared dashboard UI needs. The host must register <see cref="IKeyValueStore"/> itself.
    /// </summary>
    public static IServiceCollection AddInvestissementsClient(this IServiceCollection services, Uri apiBaseUri)
    {
        services.AddMudServices();
        services.AddApexCharts();
        services.AddLocalization();

        services.AddSingleton(new ClientOptions(apiBaseUri));
        services.AddSingleton<ILocalizationService, LocalizationService>();
        services.AddSingleton<IPrivacyModeService, PrivacyModeService>();

        CultureInfo.DefaultThreadCurrentCulture = new CultureInfo("fr-FR");
        CultureInfo.DefaultThreadCurrentUICulture = new CultureInfo("fr-FR");

        // Dedicated client without the password handler: the handler depends on ISessionService.
        services.AddSingleton<ISessionService>(sp =>
            new SessionService(new HttpClient { BaseAddress = apiBaseUri }, sp.GetRequiredService<IKeyValueStore>()));
        services.AddTransient<DashboardPasswordHandler>();

        services.AddHttpClient<IPortfolioService, PortfolioService>(client => client.BaseAddress = apiBaseUri)
            .AddHttpMessageHandler<DashboardPasswordHandler>();
        services.AddHttpClient<ISyncService, SyncService>(client => client.BaseAddress = apiBaseUri)
            .AddHttpMessageHandler<DashboardPasswordHandler>();

        services.AddScoped<DashboardViewModel>();
        services.AddScoped<TrackingViewModel>();
        services.AddScoped<LoginGateViewModel>();
        services.AddScoped<AppViewModel>();
        services.AddScoped<MainViewModel>();
        services.AddScoped<AssetViewModel>();
        services.AddScoped<AgentViewModel>();
        services.AddScoped<SettingsViewModel>();

        return services;
    }
}

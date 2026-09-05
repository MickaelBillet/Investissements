using System.Globalization;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using InvestissementsDashboard.Client;
using InvestissementsDashboard.Client.Services;
using InvestissementsDashboard.Client.ViewModels;
using MudBlazor.Services;
using ApexCharts;
using Microsoft.JSInterop;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services.AddMudServices();
builder.Services.AddApexCharts();
builder.Services.AddLocalization();
builder.Services.AddSingleton<ILocalizationService, LocalizationService>();
builder.Services.AddSingleton<IPrivacyModeService, PrivacyModeService>();

CultureInfo.DefaultThreadCurrentCulture   = new CultureInfo("fr-FR");
CultureInfo.DefaultThreadCurrentUICulture = new CultureInfo("fr-FR");

var apiBaseUrl = builder.Configuration["ApiBaseUrl"];
var apiBase = !string.IsNullOrEmpty(apiBaseUrl)
    ? new Uri(apiBaseUrl)
    : new Uri(builder.HostEnvironment.BaseAddress);

builder.Services.AddSingleton<ISessionService>(sp =>
    new SessionService(new HttpClient { BaseAddress = apiBase }, sp.GetRequiredService<IJSRuntime>()));
builder.Services.AddTransient<DashboardPasswordHandler>();

builder.Services.AddHttpClient<IPortfolioService, PortfolioService>(client =>
        client.BaseAddress = apiBase)
    .AddHttpMessageHandler<DashboardPasswordHandler>();

builder.Services.AddScoped<DashboardViewModel>();
builder.Services.AddScoped<SuiviViewModel>();

await builder.Build().RunAsync();

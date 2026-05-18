using System.Globalization;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using InvestissementsDashboard.Client;
using InvestissementsDashboard.Client.Services;
using InvestissementsDashboard.Client.ViewModels;
using MudBlazor.Services;
using ApexCharts;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services.AddMudServices();
builder.Services.AddApexCharts();
builder.Services.AddLocalization();
builder.Services.AddSingleton<ILocalizationService, LocalizationService>();

CultureInfo.DefaultThreadCurrentCulture   = new CultureInfo("fr-FR");
CultureInfo.DefaultThreadCurrentUICulture = new CultureInfo("fr-FR");

var apiBase = builder.HostEnvironment.IsDevelopment()
    ? new Uri("http://localhost:7071/")
    : new Uri(builder.HostEnvironment.BaseAddress);

builder.Services.AddHttpClient<IPortfolioService, PortfolioService>(client =>
    client.BaseAddress = apiBase);

builder.Services.AddScoped<DashboardViewModel>();
builder.Services.AddScoped<HistoryViewModel>();

await builder.Build().RunAsync();

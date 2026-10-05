using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using InvestissementsDashboard.Client;
using InvestissementsDashboard.Client.Extensions;
using InvestissementsDashboard.Client.Services;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

var apiBaseUrl = builder.Configuration["ApiBaseUrl"];
var apiBase = !string.IsNullOrEmpty(apiBaseUrl)
    ? new Uri(apiBaseUrl)
    : new Uri(builder.HostEnvironment.BaseAddress);

builder.Services.AddSingleton<IKeyValueStore, LocalStorageKeyValueStore>();
builder.Services.AddInvestissementsClient(apiBase);

await builder.Build().RunAsync();

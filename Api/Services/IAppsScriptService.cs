namespace InvestissementsDashboard.Api.Services;

internal interface IAppsScriptService
{
    Task<T?> CallAsync<T>(string service, string action, CancellationToken ct = default);
}

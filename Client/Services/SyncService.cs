using System.Net.Http.Json;
using InvestissementsDashboard.Shared.Models;

namespace InvestissementsDashboard.Client.Services;

internal sealed class SyncService(HttpClient httpClient) : ISyncService
{
    public async Task<SyncResultDto> TriggerAsync(CancellationToken ct = default)
    {
        using var response = await httpClient.PostAsync("/api/sync", null, ct);
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<SyncResultDto>(cancellationToken: ct);
        return result ?? new SyncResultDto(false, 0, null);
    }
}

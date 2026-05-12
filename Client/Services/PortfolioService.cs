using System.Net.Http.Json;
using InvestissementsDashboard.Shared.Models;

namespace InvestissementsDashboard.Client.Services;

internal sealed class PortfolioService(HttpClient httpClient) : IPortfolioService
{
    public async Task<IReadOnlyList<AssetDto>> GetAssetsAsync(CancellationToken ct = default)
    {
        var result = await httpClient.GetFromJsonAsync<AssetDto[]>("/api/assets", ct);
        return result ?? [];
    }

    public async Task<SnapshotDto?> GetLastSnapshotAsync(CancellationToken ct = default)
    {
        var response = await httpClient.GetAsync("/api/snapshot", ct);
        if (response.StatusCode == System.Net.HttpStatusCode.NotFound) return null;
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<SnapshotDto>(cancellationToken: ct);
    }

    public async Task<IReadOnlyList<SnapshotDto>> GetHistoryAsync(CancellationToken ct = default)
    {
        var result = await httpClient.GetFromJsonAsync<SnapshotDto[]>("/api/snapshot/history", ct);
        return result ?? [];
    }
}

using InvestissementsDashboard.Shared.Models;

namespace InvestissementsDashboard.Client.Services;

public interface IPortfolioService
{
    Task<IReadOnlyList<AssetDto>>    GetAssetsAsync(CancellationToken ct = default);
    Task<SnapshotDto?>               GetLastSnapshotAsync(CancellationToken ct = default);
    Task<IReadOnlyList<SnapshotDto>> GetHistoryAsync(CancellationToken ct = default);
    Task<PortfolioMetricsDto?>       GetMetricsAsync(CancellationToken ct = default);
}

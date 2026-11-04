using InvestissementsDashboard.Shared.Models;

namespace AgentAI.Core.Workflows.PortfolioReview;

/// <summary>
/// Supplies the positions, the latest snapshot and the watchlist to the portfolio review workflow.
/// </summary>
/// <remarks>
/// Only the contract lives here: the data comes from the dashboard Api, whose client (and its password handling)
/// belongs to the host, so the host provides the implementation.
/// </remarks>
public interface IAllocationSource
{
    Task<IReadOnlyList<AssetDto>> GetAssetsAsync(CancellationToken cancellationToken);

    Task<SnapshotDto?> GetLastSnapshotAsync(CancellationToken cancellationToken);

    Task<IReadOnlyList<WatchlistItemDto>> GetWatchlistAsync(CancellationToken cancellationToken);
}

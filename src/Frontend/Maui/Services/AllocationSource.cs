using AgentAI.Core.Workflows.PortfolioReview;
using InvestissementsDashboard.Client.Services;
using InvestissementsDashboard.Shared.Models;

namespace InvestissementsDashboard.Maui.Services;

/// <summary>
/// Feeds the allocation step of the portfolio review from the dashboard Api, through the same client as the dashboard
/// so that the dashboard password is sent without any extra configuration.
/// </summary>
public sealed class AllocationSource(IPortfolioService portfolioService) : IAllocationSource
{
    public Task<IReadOnlyList<AssetDto>> GetAssetsAsync(CancellationToken cancellationToken) =>
        portfolioService.GetAssetsAsync(cancellationToken);

    public Task<SnapshotDto?> GetLastSnapshotAsync(CancellationToken cancellationToken) =>
        portfolioService.GetLastSnapshotAsync(cancellationToken);

    public Task<IReadOnlyList<WatchlistItemDto>> GetWatchlistAsync(CancellationToken cancellationToken) =>
        portfolioService.GetWatchlistAsync(cancellationToken);
}

using AgentAI;
using InvestissementsDashboard.Client.Services;
using InvestissementsDashboard.Shared.Models;

namespace InvestissementsDashboard.Maui.Services;

/// <summary>
/// Feeds the portfolio review workflow from the dashboard Api, through the same client as the dashboard
/// so that the dashboard password is sent without any extra configuration.
/// </summary>
public sealed class AssetDistributionSource(IPortfolioService portfolioService) : IAssetDistributionSource
{
    public Task<IReadOnlyList<DistributionDto>> GetGeographyAsync(string assetClass, CancellationToken cancellationToken) =>
        portfolioService.GetGeographyDistributionAsync(assetClass, cancellationToken);

    public Task<IReadOnlyList<DistributionDto>> GetSectorsAsync(string assetClass, CancellationToken cancellationToken) =>
        portfolioService.GetSectorDistributionAsync(assetClass, cancellationToken);
}

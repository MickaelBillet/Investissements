using InvestissementsDashboard.Shared.Models;

namespace AgentAI;

/// <summary>
/// Supplies the geographic and sector distributions of an asset class to the portfolio review workflow.
/// </summary>
/// <remarks>
/// Only the contract lives here: the data comes from the dashboard Api, whose client (and its password handling)
/// belongs to the host, so the host provides the implementation.
/// </remarks>
public interface IAssetDistributionSource
{
    /// <param name="assetClass">"Stocks" or "Bonds", the only classes the Api distributes.</param>
    Task<IReadOnlyList<DistributionDto>> GetGeographyAsync(string assetClass, CancellationToken cancellationToken);

    /// <param name="assetClass">"Stocks" or "Bonds", the only classes the Api distributes.</param>
    Task<IReadOnlyList<DistributionDto>> GetSectorsAsync(string assetClass, CancellationToken cancellationToken);
}

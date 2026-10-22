using InvestissementsDashboard.Shared.Models;

namespace AgentAI;

/// <summary>Geographic and sector distributions of one asset class.</summary>
public record AssetClassDistribution(
    string AssetClass,
    IReadOnlyList<DistributionDto> Geography,
    IReadOnlyList<DistributionDto> Sectors);

/// <summary>Distributions of every asset class the review covers (Stocks and Bonds).</summary>
public record DistributionResult(IReadOnlyList<AssetClassDistribution> Classes);

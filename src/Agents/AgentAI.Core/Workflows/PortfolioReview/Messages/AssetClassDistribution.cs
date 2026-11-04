using InvestissementsDashboard.Shared.Models;

namespace AgentAI.Core.Workflows.PortfolioReview.Messages;

/// <summary>Geographic and sector distributions of one asset class.</summary>
public record AssetClassDistribution(
    string AssetClass,
    IReadOnlyList<DistributionDto> Geography,
    IReadOnlyList<DistributionDto> Sectors);

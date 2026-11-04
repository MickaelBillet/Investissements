namespace AgentAI.Core.Workflows.PortfolioReview.Messages;

/// <summary>Distributions of every asset class the review covers (Stocks and Bonds).</summary>
public record DistributionResult(IReadOnlyList<AssetClassDistribution> Classes);

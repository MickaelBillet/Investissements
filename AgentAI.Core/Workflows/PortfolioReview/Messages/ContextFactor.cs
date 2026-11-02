namespace AgentAI.Core.Workflows.PortfolioReview.Messages;

/// <summary>A concrete element likely to move the markets in the next 6 to 12 months.</summary>
/// <param name="AssetClasses">Labels of the application's reference list (<c>Stocks</c>, <c>Bonds</c>…), so the synthesis can match them with the portfolio.</param>
public record ContextFactor(
    string Description,
    FactorType Type,
    IReadOnlyList<string> AssetClasses,
    IReadOnlyList<string> Sectors,
    IReadOnlyList<string> Regions,
    FactorHorizon Horizon,
    IReadOnlyList<ContextSource> Sources);

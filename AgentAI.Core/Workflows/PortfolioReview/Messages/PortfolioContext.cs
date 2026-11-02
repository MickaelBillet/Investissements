namespace AgentAI.Core.Workflows.PortfolioReview.Messages;

/// <summary>
/// Economic, financial and geopolitical context at the analysis date, as established by the Context agent
/// (structured output of <c>Context.md</c>).
/// </summary>
/// <param name="Situation">Summary of the situation, organised by theme.</param>
public record PortfolioContext(
    DateOnly Date,
    string Situation,
    IReadOnlyList<ContextFactor> Factors,
    IReadOnlyList<ContextIndicator> Indicators);

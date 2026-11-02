namespace AgentAI.Core.Workflows.PortfolioReview.Messages;

public record SynthesisReport(
    IReadOnlyList<string> Warnings,
    IReadOnlyList<string> Strengths,
    IReadOnlyList<string> Weaknesses,
    IReadOnlyList<Recommendation> Recommendations);
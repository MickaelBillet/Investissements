namespace InvestissementsDashboard.Shared.Models;

/// <summary>
/// One orientation of the synthesis report (structured output of <c>Synthesis.md</c>).
/// </summary>
/// <param name="Target">Name of the asset class, theme or line, as it appears in the agent's inputs.</param>
/// <param name="Driver">The scenario or context factor that justifies it.</param>
public record Recommendation(
    RecommendationLevel Level,
    string Target,
    RecommendationDirection Direction,
    string Driver,
    string Rationale,
    RecommendationConfidence Confidence);

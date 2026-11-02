using System.Text.Json.Serialization;

namespace AgentAI.Core.Workflows.PortfolioReview.Messages;

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

// The enums are written as names in the JSON schema sent to the model, which reads and writes them as text.

[JsonConverter(typeof(JsonStringEnumConverter<RecommendationLevel>))]
public enum RecommendationLevel
{
    AssetClass,
    Theme,
    Line,
}

[JsonConverter(typeof(JsonStringEnumConverter<RecommendationDirection>))]
public enum RecommendationDirection
{
    Reinforce,
    Reduce,
    Initiate,
    Watch,
}

[JsonConverter(typeof(JsonStringEnumConverter<RecommendationConfidence>))]
public enum RecommendationConfidence
{
    Low,
    Medium,
    High,
}

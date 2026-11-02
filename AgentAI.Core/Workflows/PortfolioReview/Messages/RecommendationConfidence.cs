using System.Text.Json.Serialization;

namespace AgentAI.Core.Workflows.PortfolioReview.Messages;

// Written as a name in the JSON schema sent to the model, which reads and writes it as text.
[JsonConverter(typeof(JsonStringEnumConverter<RecommendationConfidence>))]
public enum RecommendationConfidence
{
    Low,
    Medium,
    High,
}

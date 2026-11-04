using System.Text.Json.Serialization;

namespace AgentAI.Core.Workflows.PortfolioReview.Messages;

// Written as a name in the JSON schema sent to the model, which reads and writes it as text.
[JsonConverter(typeof(JsonStringEnumConverter<FactorHorizon>))]
public enum FactorHorizon
{
    /// <summary>Less than 6 months.</summary>
    ShortTerm,

    /// <summary>6 to 18 months.</summary>
    MediumTerm,
}

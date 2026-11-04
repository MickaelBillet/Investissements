using System.Text.Json.Serialization;

namespace InvestissementsDashboard.Shared.Models;

// Written as a name in the JSON schema sent to the model, which reads and writes it as text.
[JsonConverter(typeof(JsonStringEnumConverter<ScenarioVerdict>))]
public enum ScenarioVerdict
{
    Confirmed,
    Weakened,
    Neutral,
}

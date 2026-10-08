using Microsoft.Agents.AI;

namespace AgentAI;

/// <summary>
/// Injects the requested city into the agent's context, so the user only has to type the city name
/// rather than a full free-text question.
/// </summary>
public sealed class WeatherContextProvider(string city) : AIContextProvider
{
    protected override ValueTask<AIContext> ProvideAIContextAsync(
        InvokingContext context, CancellationToken ct = default)
    {
        return ValueTask.FromResult(new AIContext
        {
            Instructions = $"Ville demandée : {city}.",
        });
    }
}

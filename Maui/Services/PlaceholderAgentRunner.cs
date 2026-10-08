using InvestissementsDashboard.Client.Services;

namespace InvestissementsDashboard.Maui.Services;

/// <summary>
/// Temporary stand-in so the agent UI can be exercised before the real agents are wired in.
/// </summary>
public sealed class PlaceholderAgentRunner : IAgentRunner
{
    public async Task<string> RunAsync(AgentChoice agent, string assetName, CancellationToken cancellationToken = default)
    {
        await Task.Delay(TimeSpan.FromSeconds(1.5), cancellationToken);
        return $"[Réponse fictive] Agent {agent} pour « {assetName} ».";
    }
}

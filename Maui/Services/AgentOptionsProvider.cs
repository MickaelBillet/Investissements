using AgentAI;

namespace InvestissementsDashboard.Maui.Services;

/// <summary>
/// Builds the <see cref="AgentAIOptions"/> from the configuration kept in the Api. Read on every launch, so a change
/// of App Setting applies without restarting or republishing the application.
/// </summary>
public sealed class AgentOptionsProvider(IAgentConfigSource configSource, Uri apiBaseUri, string historyDirectory)
{
    /// <exception cref="InvalidOperationException">The Foundry endpoint is not configured in the Api.</exception>
    public async Task<AgentAIOptions> CreateAsync(CancellationToken cancellationToken)
    {
        var config = await configSource.GetAsync(cancellationToken);
        if (string.IsNullOrWhiteSpace(config.FoundryEndpoint))
        {
            throw new InvalidOperationException("The Foundry endpoint is not configured in the Api (App Setting AGENT_FOUNDRY_ENDPOINT).");
        }

        return new AgentAIOptions
        {
            FoundryProjectEndpoint = config.FoundryEndpoint,
            Model = string.IsNullOrWhiteSpace(config.Model) ? AgentAIOptions.DefaultModel : config.Model,
            // The MCP server is the Api itself: same origin, same dashboard password.
            InvestZaptoMcpUrl = new Uri(apiBaseUri, "api/mcp").ToString(),
            HistoryDirectory = historyDirectory,
        };
    }
}

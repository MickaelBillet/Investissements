using AgentAI;
using InvestissementsDashboard.Client.Services;

namespace InvestissementsDashboard.Maui.Services;

/// <summary>
/// Builds the <see cref="AgentAIOptions"/> from the user's settings. Called lazily (see <c>AddAgentAI</c> with a factory),
/// so a missing endpoint is reported when an agent is launched rather than preventing the app from starting.
/// </summary>
public sealed class AgentOptionsProvider(IAgentSettings settings, Uri apiBaseUri, string historyDirectory)
{
    public AgentAIOptions Create()
    {
        var endpoint = settings.FoundryEndpoint;
        if (string.IsNullOrWhiteSpace(endpoint))
        {
            throw new InvalidOperationException("The Foundry endpoint is not configured: set it in the settings page.");
        }

        var model = settings.Model;
        return new AgentAIOptions
        {
            FoundryProjectEndpoint = endpoint,
            Model = string.IsNullOrWhiteSpace(model) ? AgentAIOptions.DefaultModel : model,
            // The stored base URL wins; the Api origin is the fallback if the preference was cleared or is malformed.
            InvestZaptoMcpUrl = new Uri(ResolveBaseUri(), "api/mcp").ToString(),
            HistoryDirectory = historyDirectory,
        };
    }

    private Uri ResolveBaseUri() =>
        Uri.TryCreate(settings.InvestZaptoBaseUrl, UriKind.Absolute, out var stored) ? stored : apiBaseUri;
}

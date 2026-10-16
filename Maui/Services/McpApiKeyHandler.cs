using InvestissementsDashboard.Client.Services;

namespace InvestissementsDashboard.Maui.Services;

/// <summary>
/// Adds the MCP key (kept encrypted in <see cref="IKeyValueStore"/>) to the requests of the MCP client only, so it never
/// reaches the RSS feeds or the dashboard Api. The key is read on every request: a change in the settings page applies
/// without restarting, although the MCP client is captured by a singleton.
/// </summary>
public sealed class McpApiKeyHandler(IKeyValueStore store) : DelegatingHandler
{
    internal const string HeaderName = "x-mcp-api-key";

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var key = await store.GetAsync(AgentSecretKeys.McpApiKey);
        if (!string.IsNullOrWhiteSpace(key) && !request.Headers.Contains(HeaderName))
        {
            request.Headers.Add(HeaderName, key);
        }

        return await base.SendAsync(request, cancellationToken);
    }
}

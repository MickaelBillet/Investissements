namespace InvestissementsDashboard.Client.Services;

/// <summary>
/// Keys under which the agent secrets are kept in <see cref="IKeyValueStore"/> (encrypted by the OS on MAUI).
/// Shared between the settings page, which writes them, and the host, which reads them. They are not part of
/// <see cref="IAgentSettings"/>, whose values are stored in clear text.
/// </summary>
public static class AgentSecretKeys
{
    /// <summary>Key sent as <c>x-mcp-api-key</c> to the MCP endpoint (the Api's <c>MCP_API_KEY</c> setting).</summary>
    public const string McpApiKey = "agent.mcp.apikey";
}

namespace AgentAI;

/// <summary>
/// Host-provided configuration for <see cref="AgentFactory"/>.
/// </summary>
/// <remarks>
/// The library never reads environment variables itself: a MAUI host has no meaningful process
/// environment, so each host (console, MAUI…) decides where these values come from.
/// </remarks>
public sealed class AgentAIOptions
{
    /// <summary>Azure AI Foundry project endpoint.</summary>
    public required string FoundryProjectEndpoint { get; init; }

    /// <summary>Model used when the host does not configure one.</summary>
    public const string DefaultModel = "gpt-5-mini";

    /// <summary>Model deployment name used by every agent.</summary>
    public string Model { get; init; } = DefaultModel;

    /// <summary>InvestZapto MCP server URL; only required by <see cref="AgentKind.Portfolio"/>.</summary>
    public string? InvestZaptoMcpUrl { get; init; }

    /// <summary>
    /// Writable directory where per-agent chat history files are persisted. Must be provided by the host
    /// because the app base directory is not writable on every platform (e.g. packaged MAUI apps).
    /// </summary>
    public required string HistoryDirectory { get; init; }
}

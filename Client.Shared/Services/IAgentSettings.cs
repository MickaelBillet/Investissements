namespace InvestissementsDashboard.Client.Services;

/// <summary>
/// User-editable configuration of the AI agents. Deliberately not registered by <c>AddInvestissementsClient</c>:
/// only the MAUI host provides an implementation (agents never run in the WASM site), which also hides the settings page there.
/// </summary>
/// <remarks>
/// Holds non-secret values only (the Foundry endpoint is protected by Entra ID, not by its secrecy);
/// real secrets belong in <see cref="IKeyValueStore"/>.
/// </remarks>
public interface IAgentSettings
{
    /// <summary>Azure AI Foundry project endpoint, or <see langword="null"/> when not configured yet.</summary>
    string? FoundryEndpoint { get; }

    /// <summary>Model deployment name, or <see langword="null"/> to use the library default.</summary>
    string? Model { get; }

    /// <summary>
    /// Base URL of the InvestZapto site hosting the MCP endpoint, or <see langword="null"/> when the host has none.
    /// Read-only on purpose: the host seeds it, the settings page does not edit it.
    /// </summary>
    string? InvestZaptoBaseUrl { get; }

    void Save(string? foundryEndpoint, string? model);
}

namespace InvestissementsDashboard.Shared.Models;

/// <summary>
/// Configuration of the AI agents of the MAUI application, served by <c>GET /api/agents/config</c>. Not secret: the
/// Foundry endpoint is protected by Entra ID. <see langword="null"/> when the matching App Setting is not configured.
/// </summary>
public record AgentConfigDto(
    string? FoundryEndpoint,
    string? Model
);

using System.Net.Http.Json;
using InvestissementsDashboard.Shared.Models;

namespace InvestissementsDashboard.Maui.Services;

/// <summary>
/// Reads the agents' configuration from <c>GET /api/agents/config</c>, through a typed client that sends the dashboard
/// password (<c>DashboardPasswordHandler</c>) like the dashboard clients.
/// </summary>
public sealed class ApiAgentConfigSource(HttpClient httpClient) : IAgentConfigSource
{
    internal const string Route = "/api/agents/config";

    public async Task<AgentConfigDto> GetAsync(CancellationToken cancellationToken)
    {
        using var response = await httpClient.GetAsync(Route, cancellationToken);
        response.EnsureSuccessStatusCode();

        return await response.Content.ReadFromJsonAsync<AgentConfigDto>(cancellationToken) ?? new AgentConfigDto(null, null);
    }
}

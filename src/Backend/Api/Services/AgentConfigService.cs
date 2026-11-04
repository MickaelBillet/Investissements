using InvestissementsDashboard.Shared.Models;
using Microsoft.Extensions.Configuration;

namespace InvestissementsDashboard.Api.Services;

/// <summary>
/// Serves the agents' configuration from the App Settings, so it is set once here instead of on every PC running
/// the MAUI application. Only non-secret values belong here.
/// </summary>
internal sealed class AgentConfigService : IAgentConfigService
{
    private readonly IConfiguration _configuration;

    public AgentConfigService(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public AgentConfigDto Get() =>
        new(Read("AGENT_FOUNDRY_ENDPOINT"), Read("AGENT_FOUNDRY_MODEL"));

    // Blank means "not configured": the application then reports it (endpoint) or uses its default (model).
    private string? Read(string key)
    {
        var value = _configuration[key];
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }
}

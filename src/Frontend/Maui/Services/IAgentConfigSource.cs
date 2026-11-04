using InvestissementsDashboard.Shared.Models;

namespace InvestissementsDashboard.Maui.Services;

/// <summary>Supplies the agents' configuration, kept in the Api's App Settings rather than on each PC.</summary>
public interface IAgentConfigSource
{
    /// <exception cref="HttpRequestException">The Api could not be reached or refused the request (401: wrong dashboard password).</exception>
    Task<AgentConfigDto> GetAsync(CancellationToken cancellationToken);
}

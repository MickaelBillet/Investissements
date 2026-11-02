using InvestissementsDashboard.Shared.Models;

namespace AgentAI;

/// <summary>
/// Searches the public web, so that an agent can ground its answer on facts more recent than its training data.
/// </summary>
/// <remarks>
/// Contract only, implemented by the host: the provider's key must stay server-side, so the MAUI host goes through
/// the dashboard Api (<c>POST /api/websearch</c>).
/// </remarks>
public interface IWebSearchService
{
    /// <returns>The pages found, most relevant first; empty when nothing matches.</returns>
    /// <exception cref="HttpRequestException">The search could not be performed (Api or provider unreachable, refused, quota exhausted).</exception>
    Task<IReadOnlyList<WebSearchResultDto>> SearchAsync(string query, WebSearchTopic topic, WebSearchTimeRange timeRange, CancellationToken cancellationToken);
}

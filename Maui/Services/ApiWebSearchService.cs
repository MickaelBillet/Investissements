using System.Net.Http.Json;
using AgentAI;
using InvestissementsDashboard.Shared.Models;
using Microsoft.Extensions.Logging;

namespace InvestissementsDashboard.Maui.Services;

/// <summary>
/// Runs the agents' web searches through the dashboard Api, which holds the search provider's key: the application
/// only sends the dashboard password, added by the <c>DashboardPasswordHandler</c> of this typed client.
/// </summary>
public sealed class ApiWebSearchService(HttpClient httpClient, ILogger<ApiWebSearchService> logger) : IWebSearchService
{
    internal const string Route = "/api/websearch";

    public async Task<IReadOnlyList<WebSearchResultDto>> SearchAsync(string query, WebSearchTopic topic, WebSearchTimeRange timeRange, CancellationToken cancellationToken)
    {
        try
        {
            using var response = await httpClient.PostAsJsonAsync(Route, new WebSearchRequestDto(query, topic, timeRange), cancellationToken);
            response.EnsureSuccessStatusCode();

            var results = await response.Content.ReadFromJsonAsync<List<WebSearchResultDto>>(cancellationToken) ?? [];
            logger.LogInformation("Web search '{Query}' ({Topic}, {TimeRange}): {Count} results", query, topic, timeRange, results.Count);
            return results;
        }
        catch (HttpRequestException ex)
        {
            // Logged here because the agent framework hands tool errors to the model, not to the runner.
            logger.LogWarning(ex, "Web search '{Query}' failed (status {Status})", query, (int?)ex.StatusCode);
            throw;
        }
    }
}

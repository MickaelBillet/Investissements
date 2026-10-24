using InvestissementsDashboard.Shared.Models;

namespace InvestissementsDashboard.Api.Services;

public interface IWebSearchService
{
    /// <returns>The pages found, most relevant first; empty when nothing matches.</returns>
    /// <exception cref="HttpRequestException">The provider could not be reached, or refused the request (wrong key, exhausted quota).</exception>
    /// <exception cref="InvalidOperationException"><c>WEB_SEARCH_API_KEY</c> is not configured.</exception>
    Task<IReadOnlyList<WebSearchResultDto>> SearchAsync(WebSearchRequestDto request, CancellationToken ct = default);
}

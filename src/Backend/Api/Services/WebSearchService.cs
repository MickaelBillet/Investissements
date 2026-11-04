using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using InvestissementsDashboard.Shared.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace InvestissementsDashboard.Api.Services;

/// <summary>
/// Web search for the agents of the MAUI application, backed by the Tavily search API. Runs here rather than in the
/// application so that the provider's key (<c>WEB_SEARCH_API_KEY</c>) never leaves the Api.
/// </summary>
internal sealed class WebSearchService : IWebSearchService
{
    internal static readonly Uri Endpoint = new("https://api.tavily.com/search");

    // Enough for the model to cross-check a fact while keeping each tool result short in its prompt.
    internal const int MaxResults = 5;

    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;
    private readonly ILogger<WebSearchService> _logger;

    public WebSearchService(HttpClient httpClient, IConfiguration configuration, ILogger<WebSearchService> logger)
    {
        _httpClient    = httpClient;
        _configuration = configuration;
        _logger        = logger;
    }

    public async Task<IReadOnlyList<WebSearchResultDto>> SearchAsync(WebSearchRequestDto request, CancellationToken ct = default)
    {
        var key = _configuration["WEB_SEARCH_API_KEY"];
        if (string.IsNullOrEmpty(key))
        {
            _logger.LogError("WEB_SEARCH_API_KEY is not configured.");
            throw new InvalidOperationException("WEB_SEARCH_API_KEY is not configured.");
        }

        var body = new TavilyRequest(
            Query: request.Query,
            Topic: request.Topic.ToString().ToLowerInvariant(),
            // "basic" costs one credit, "advanced" two: the extracts of a basic search are enough to cite a figure.
            SearchDepth: "basic",
            MaxResults: MaxResults,
            TimeRange: request.TimeRange == WebSearchTimeRange.Any ? null : request.TimeRange.ToString().ToLowerInvariant(),
            IncludePublishedDate: true);

        // The key goes on the request, not on the client: the client is shared through IHttpClientFactory.
        using var message = new HttpRequestMessage(HttpMethod.Post, Endpoint) { Content = JsonContent.Create(body) };
        message.Headers.Authorization = new AuthenticationHeaderValue("Bearer", key);

        using var response = await _httpClient.SendAsync(message, ct);
        if (!response.IsSuccessStatusCode)
        {
            // 401: wrong key; 432/433: monthly quota exhausted.
            _logger.LogError("Web search failed with status {Status}.", (int)response.StatusCode);
            throw new HttpRequestException($"Web search failed with status {(int)response.StatusCode}.", null, response.StatusCode);
        }

        var payload = await response.Content.ReadFromJsonAsync<TavilyResponse>(ct);
        return (payload?.Results ?? [])
            .Select(r => new WebSearchResultDto(r.Title ?? string.Empty, r.Url ?? string.Empty, r.Content ?? string.Empty, r.PublishedDate))
            .ToList();
    }

    private sealed record TavilyRequest(
        [property: JsonPropertyName("query")] string Query,
        [property: JsonPropertyName("topic")] string Topic,
        [property: JsonPropertyName("search_depth")] string SearchDepth,
        [property: JsonPropertyName("max_results")] int MaxResults,
        [property: JsonPropertyName("time_range"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] string? TimeRange,
        [property: JsonPropertyName("include_published_date")] bool IncludePublishedDate);

    private sealed record TavilyResponse(
        [property: JsonPropertyName("results")] IReadOnlyList<TavilyResult>? Results);

    private sealed record TavilyResult(
        [property: JsonPropertyName("title")] string? Title,
        [property: JsonPropertyName("url")] string? Url,
        [property: JsonPropertyName("content")] string? Content,
        [property: JsonPropertyName("published_date")] string? PublishedDate);
}

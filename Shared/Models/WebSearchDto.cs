using System.Text.Json.Serialization;

namespace InvestissementsDashboard.Shared.Models;

/// <summary>Kind of sources a web search favours.</summary>
// Names rather than numbers in JSON: the agents' model reads and writes them, and so does the Api request.
[JsonConverter(typeof(JsonStringEnumConverter<WebSearchTopic>))]
public enum WebSearchTopic
{
    General,
    News,
    Finance,
}

/// <summary>How recent the pages returned by a web search must be.</summary>
[JsonConverter(typeof(JsonStringEnumConverter<WebSearchTimeRange>))]
public enum WebSearchTimeRange
{
    Any,
    Day,
    Week,
    Month,
    Year,
}

/// <summary>Body of <c>POST /api/websearch</c>.</summary>
public record WebSearchRequestDto(
    string Query,
    WebSearchTopic Topic = WebSearchTopic.General,
    WebSearchTimeRange TimeRange = WebSearchTimeRange.Any
);

/// <summary>One page found by a web search, reduced to what an agent needs to read and cite it.</summary>
/// <param name="Content">Extract of the page relevant to the query, not the full page.</param>
/// <param name="PublishedDate">Publication date as reported by the provider, or <see langword="null"/> when it is unknown.</param>
public record WebSearchResultDto(
    string Title,
    string Url,
    string Content,
    string? PublishedDate
);

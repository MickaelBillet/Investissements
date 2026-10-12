using Microsoft.Extensions.Logging;

namespace AgentAI;

/// <summary>
/// Google News search feed, keyed on the company name.
/// </summary>
public sealed class GoogleNewsFeedSource : INewsFeedSource
{
    private readonly IRssFeedReader _reader;
    private readonly ILogger<GoogleNewsFeedSource> _logger;

    public GoogleNewsFeedSource(IRssFeedReader reader, ILogger<GoogleNewsFeedSource> logger)
    {
        ArgumentNullException.ThrowIfNull(reader);
        ArgumentNullException.ThrowIfNull(logger);

        _reader = reader;
        _logger = logger;
    }

    public string Name => "Google News";

    public async Task<IReadOnlyList<NewsArticle>?> FetchAsync(NewsRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var articles = await _reader.ReadAsync(this.BuildUrl(request.Company, request.Days), Name, cancellationToken).ConfigureAwait(false);

        // Google News matches the phrase anywhere in the article body, hence the title check.
        var kept = CompanyTitleFilter.Filter(articles, request.Company);
        _logger.LogDebug("Dropped {Dropped} of {Total} articles whose title does not name '{Company}'.", articles.Count - kept.Count, articles.Count, request.Company);
        return kept;
    }

    private Uri BuildUrl(string company, int days)
    {
        // "when:Nd" restricts results server-side; the exact-phrase quotes avoid matching unrelated
        // articles that merely contain one word of the company name.
        var query = Uri.EscapeDataString($"\"{company}\" when:{days}d");
        return new Uri($"https://news.google.com/rss/search?q={query}&hl=fr&gl=FR&ceid=FR:fr");
    }
}

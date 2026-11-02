using Microsoft.Extensions.Logging;

namespace AgentAI;

/// <summary>
/// Merges the news of every <see cref="INewsFeedSource"/> into one list: most recent first, without
/// duplicates, limited to the look-back window.
/// </summary>
/// <remarks>
/// The feeds are free and keyless but undocumented: their format or terms may change, and coverage is
/// not exhaustive. Each source is queried independently so one failing source does not hide the others.
/// </remarks>
public sealed class NewsService : INewsService
{
    private const int MaxArticles = 30;

    private readonly IReadOnlyList<INewsFeedSource> _sources;
    private readonly TimeProvider _timeProvider;
    private readonly ILogger<NewsService> _logger;

    public NewsService(IEnumerable<INewsFeedSource> sources, TimeProvider timeProvider, ILogger<NewsService> logger)
    {
        ArgumentNullException.ThrowIfNull(sources);
        ArgumentNullException.ThrowIfNull(timeProvider);
        ArgumentNullException.ThrowIfNull(logger);

        _sources = sources.ToList();
        _timeProvider = timeProvider;
        _logger = logger;
    }

    public async Task<IReadOnlyList<NewsArticle>> GetLatestNewsAsync(
        string company,
        string? isin = null,
        int days = 7,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(company);
        ArgumentOutOfRangeException.ThrowIfLessThan(days, 1);

        var request = new NewsRequest(company, isin, days);
        var results = await Task.WhenAll(_sources.Select(source => TryFetchAsync(source, request, cancellationToken))).ConfigureAwait(false);

        // Distinguishing "no news" from "no answer" matters: the agent must not report an empty week
        // when in reality every source was unreachable.
        var queried = results.Where(result => result.Applicable).ToList();
        if (queried.Count > 0 && queried.All(result => result.Articles is null))
        {
            throw new HttpRequestException($"All news feeds failed for '{company}'. See previous log entries for details.");
        }

        var threshold = _timeProvider.GetUtcNow().AddDays(-days);

        return queried
            .Where(result => result.Articles is not null)
            .SelectMany(result => result.Articles!)
            .Where(article => article.PublishedUtc >= threshold)
            .DistinctBy(article => article.Url)
            .DistinctBy(article => NormalizeTitle(article.Title))
            .OrderByDescending(article => article.PublishedUtc)
            .Take(MaxArticles)
            .ToList();
    }

    /// <summary>
    /// Queries one source; a failure is logged and reported as "applicable but no answer" instead of
    /// being thrown, so the other sources can still contribute.
    /// </summary>
    private async Task<SourceResult> TryFetchAsync(INewsFeedSource source, NewsRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var articles = await source.FetchAsync(request, cancellationToken).ConfigureAwait(false);
            return articles is null ? SourceResult.NotApplicable : new SourceResult(true, articles);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogWarning(ex, "News source {Source} could not be read.", source.Name);
            return new SourceResult(true, null);
        }
    }

    /// <summary>
    /// Lets the same story syndicated by two feeds collapse to one entry even though the URLs differ.
    /// </summary>
    private string NormalizeTitle(string title)
    {
        return string.Concat(title.Where(char.IsLetterOrDigit)).ToUpperInvariant();
    }

    /// <param name="Applicable">False when the source declined the request (it must not count as a failure).</param>
    /// <param name="Articles"><c>null</c> when the source was applicable but failed.</param>
    private readonly record struct SourceResult(bool Applicable, IReadOnlyList<NewsArticle>? Articles)
    {
        public static SourceResult NotApplicable => new(false, null);
    }
}

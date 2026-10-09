using System.Globalization;
using System.Text;
using System.Xml;
using System.Xml.Linq;
using Microsoft.Extensions.Logging;

namespace AgentAI;

/// <summary>
/// Reads company news from unofficial RSS endpoints (Google News, and Yahoo Finance when a ticker is known).
/// </summary>
/// <remarks>
/// These feeds are free and keyless but undocumented: their format or terms may change, and coverage is
/// not exhaustive. Each feed is queried independently so one failing source does not hide the others.
/// </remarks>
public sealed class RssNewsService : INewsService
{
    #region Fields
    private const int MaxArticles = 30;
    private const string YahooSourceName = "Yahoo Finance";

    // Dropped from the end of a company name before matching titles: headlines rarely carry the legal form.
    private static readonly string[] LegalSuffixes = ["nv", "sa", "se", "ag", "plc", "inc", "ltd", "corp", "corporation", "spa", "srl", "gmbh", "oyj", "ab"];

    private readonly HttpClient _httpClient;
    private readonly TimeProvider _timeProvider;
    private readonly ILogger<RssNewsService> _logger;
    #endregion

    #region Constructor
    public RssNewsService(HttpClient httpClient, TimeProvider timeProvider, ILogger<RssNewsService> logger)
    {
        ArgumentNullException.ThrowIfNull(httpClient);
        ArgumentNullException.ThrowIfNull(timeProvider);
        ArgumentNullException.ThrowIfNull(logger);

        _httpClient = httpClient;
        _timeProvider = timeProvider;
        _logger = logger;
    }
    #endregion

    #region Methods
    public async Task<IReadOnlyList<NewsArticle>> GetLatestNewsAsync(
        string company,
        string? ticker = null,
        int days = 7,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(company);
        ArgumentOutOfRangeException.ThrowIfLessThan(days, 1);

        var feedTasks = new List<Task<IReadOnlyList<NewsArticle>?>>
        {
            TryFetchFeedAsync(BuildGoogleNewsUrl(company, days), defaultSource: "Google News", cancellationToken),
        };

        if (!string.IsNullOrWhiteSpace(ticker))
        {
            feedTasks.Add(TryFetchFeedAsync(BuildYahooFinanceUrl(ticker), defaultSource: YahooSourceName, cancellationToken));
        }

        var feeds = await Task.WhenAll(feedTasks).ConfigureAwait(false);

        // Distinguishing "no news" from "no answer" matters: the agent must not report an empty week
        // when in reality every source was unreachable.
        if (feeds.All(feed => feed is null))
        {
            throw new HttpRequestException($"All news feeds failed for '{company}'. See previous log entries for details.");
        }

        // Google News matches the phrase anywhere in the article body, so some results never name the company in
        // their title; the agent only sees titles and would have to flag them as ambiguous. The Yahoo feed is
        // already scoped by ticker, so it is left untouched.
        if (feeds[0] is { } googleArticles)
        {
            feeds[0] = FilterByCompanyInTitle(googleArticles, company);
        }

        var threshold = _timeProvider.GetUtcNow().AddDays(-days);

        return feeds
            .Where(feed => feed is not null)
            .SelectMany(feed => feed!)
            .Where(article => article.PublishedUtc >= threshold)
            .DistinctBy(article => article.Url)
            .DistinctBy(article => NormalizeTitle(article.Title))
            .OrderByDescending(article => article.PublishedUtc)
            .Take(MaxArticles)
            .ToList();
    }

    /// <summary>
    /// Fetches and parses one feed; returns <c>null</c> (after logging) instead of throwing so the
    /// other feeds can still contribute.
    /// </summary>
    private async Task<IReadOnlyList<NewsArticle>?> TryFetchFeedAsync(Uri url, string defaultSource, CancellationToken cancellationToken)
    {
        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, url);

            // Some feed endpoints reject requests that carry no User-Agent.
            request.Headers.UserAgent.ParseAdd("Mozilla/5.0 (compatible; AgentAI)");

            using var response = await _httpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);
            response.EnsureSuccessStatusCode();

            var content = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
            return ParseFeed(content, defaultSource);
        }
        catch (Exception ex) when (ex is HttpRequestException or XmlException)
        {
            _logger.LogWarning(ex, "News feed {FeedUrl} could not be read.", url);
            return null;
        }
    }

    private IReadOnlyList<NewsArticle> ParseFeed(string xml, string defaultSource)
    {
        // DTD processing is prohibited: the feed comes from a third party and must not be able to
        // trigger entity expansion attacks.
        var settings = new XmlReaderSettings { DtdProcessing = DtdProcessing.Prohibit, XmlResolver = null };
        using var stringReader = new StringReader(xml);
        using var xmlReader = XmlReader.Create(stringReader, settings);
        var document = XDocument.Load(xmlReader);

        var articles = new List<NewsArticle>();
        foreach (var item in document.Descendants("item"))
        {
            var title = item.Element("title")?.Value.Trim();
            var link = item.Element("link")?.Value.Trim();
            var pubDate = item.Element("pubDate")?.Value;

            if (string.IsNullOrEmpty(title)
                || string.IsNullOrEmpty(link)
                || !DateTimeOffset.TryParse(pubDate, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out var published))
            {
                // An item without title, link or a readable date cannot be placed in the time window.
                continue;
            }

            var source = item.Element("source")?.Value.Trim();
            if (string.IsNullOrEmpty(source))
            {
                source = defaultSource;
            }

            articles.Add(new NewsArticle(StripSourceSuffix(title, source), source, published.ToUniversalTime(), link));
        }

        return articles;
    }

    private Uri BuildGoogleNewsUrl(string company, int days)
    {
        // "when:Nd" restricts results server-side; the exact-phrase quotes avoid matching unrelated
        // articles that merely contain one word of the company name.
        var query = Uri.EscapeDataString($"\"{company}\" when:{days}d");
        return new Uri($"https://news.google.com/rss/search?q={query}&hl=fr&gl=FR&ceid=FR:fr");
    }

    private Uri BuildYahooFinanceUrl(string ticker)
    {
        return new Uri($"https://feeds.finance.yahoo.com/rss/2.0/headline?s={Uri.EscapeDataString(ticker.Trim())}&region=US&lang=en-US");
    }

    /// <summary>
    /// Google News appends " - Publisher" to every headline; it is already exposed through
    /// <see cref="NewsArticle.Source"/>, so keeping it would duplicate it in the agent's output.
    /// </summary>
    private string StripSourceSuffix(string title, string source)
    {
        var suffix = $" - {source}";
        return title.EndsWith(suffix, StringComparison.OrdinalIgnoreCase)
            ? title[..^suffix.Length].TrimEnd()
            : title;
    }

    /// <summary>
    /// Lets the same story syndicated by two feeds collapse to one entry even though the URLs differ.
    /// </summary>
    private string NormalizeTitle(string title)
    {
        return string.Concat(title.Where(char.IsLetterOrDigit)).ToUpperInvariant();
    }

    private IReadOnlyList<NewsArticle> FilterByCompanyInTitle(IReadOnlyList<NewsArticle> articles, string company)
    {
        var words = NormalizeForMatch(company).Split(' ', StringSplitOptions.RemoveEmptyEntries).ToList();
        while (words.Count > 1 && LegalSuffixes.Contains(words[^1]))
        {
            words.RemoveAt(words.Count - 1);
        }

        if (words.Count == 0)
        {
            // Nothing usable to match on: keeping everything beats silently dropping every article.
            return articles;
        }

        // Padding with spaces makes the match whole-word, so "ARM" does not match "farm".
        var needle = $" {string.Join(' ', words)} ";
        var kept = articles.Where(article => $" {NormalizeForMatch(article.Title)} ".Contains(needle, StringComparison.Ordinal)).ToList();

        _logger.LogDebug("Dropped {Dropped} of {Total} articles whose title does not name '{Company}'.", articles.Count - kept.Count, articles.Count, company);
        return kept;
    }

    /// <summary>
    /// Lowercases, strips accents and turns every non-alphanumeric character into a single space.
    /// </summary>
    private static string NormalizeForMatch(string text)
    {
        var builder = new StringBuilder(text.Length);
        foreach (var c in text.Normalize(NormalizationForm.FormD))
        {
            if (CharUnicodeInfo.GetUnicodeCategory(c) == UnicodeCategory.NonSpacingMark)
            {
                continue;
            }

            builder.Append(char.IsLetterOrDigit(c) ? char.ToLowerInvariant(c) : ' ');
        }

        return string.Join(' ', builder.ToString().Split(' ', StringSplitOptions.RemoveEmptyEntries));
    }
    #endregion
}

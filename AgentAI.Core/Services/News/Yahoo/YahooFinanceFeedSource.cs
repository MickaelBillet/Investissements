namespace AgentAI;

/// <summary>
/// Yahoo Finance headline feed, keyed on the ticker resolved from the ISIN. Already scoped to the
/// security, so its articles are not filtered on the title.
/// </summary>
public sealed class YahooFinanceFeedSource : INewsFeedSource
{
    private readonly YahooTickerResolver _tickerResolver;
    private readonly IRssFeedReader _reader;

    public YahooFinanceFeedSource(YahooTickerResolver tickerResolver, IRssFeedReader reader)
    {
        ArgumentNullException.ThrowIfNull(tickerResolver);
        ArgumentNullException.ThrowIfNull(reader);

        _tickerResolver = tickerResolver;
        _reader = reader;
    }

    public string Name => "Yahoo Finance";

    public async Task<IReadOnlyList<NewsArticle>?> FetchAsync(NewsRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var ticker = await _tickerResolver.ResolveAsync(request.Isin, cancellationToken).ConfigureAwait(false);
        if (ticker is null)
        {
            // Not applicable rather than failed: without a ticker this source simply has nothing to ask.
            return null;
        }

        return await _reader.ReadAsync(BuildUrl(ticker), Name, cancellationToken).ConfigureAwait(false);
    }

    private static Uri BuildUrl(string ticker)
    {
        return new Uri($"https://feeds.finance.yahoo.com/rss/2.0/headline?s={Uri.EscapeDataString(ticker)}&region=US&lang=en-US");
    }
}

using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;

namespace AgentAI;

/// <summary>
/// Resolves an ISIN through Yahoo Finance's (undocumented) search endpoint.
/// </summary>
/// <remarks>
/// The Yahoo headline feed only understands tickers: an ISIN silently yields an empty feed.
/// </remarks>
public sealed class YahooTickerResolver
{
    // Structural check only (country code + 9 alphanumerics + check digit): keeps arbitrary text out of the search URL.
    private static readonly Regex IsinFormat = new("^[A-Z]{2}[A-Z0-9]{9}[0-9]$", RegexOptions.Compiled | RegexOptions.CultureInvariant);

    private readonly HttpClient _httpClient;
    private readonly ILogger<YahooTickerResolver> _logger;

    public YahooTickerResolver(HttpClient httpClient, ILogger<YahooTickerResolver> logger)
    {
        ArgumentNullException.ThrowIfNull(httpClient);
        ArgumentNullException.ThrowIfNull(logger);

        _httpClient = httpClient;
        _logger = logger;
    }

    /// <returns>
    /// The ticker, or <c>null</c> when none can be found (blank, malformed or unknown ISIN, lookup failure).
    /// Never throws for those cases: the lookup is a best-effort enrichment.
    /// </returns>
    public async Task<string?> ResolveAsync(string? isin, CancellationToken cancellationToken)
    {
        var candidate = isin?.Trim().ToUpperInvariant();
        if (string.IsNullOrEmpty(candidate))
        {
            return null;
        }

        if (!IsinFormat.IsMatch(candidate))
        {
            _logger.LogWarning("'{Isin}' is not a valid ISIN; Yahoo Finance is skipped.", isin);
            return null;
        }

        try
        {
            var url = new Uri($"https://query1.finance.yahoo.com/v1/finance/search?q={candidate}&quotesCount=5&newsCount=0");
            using var request = new HttpRequestMessage(HttpMethod.Get, url);
            request.Headers.UserAgent.ParseAdd("Mozilla/5.0 (compatible; AgentAI)");

            using var response = await _httpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
            var ticker = this.ExtractTicker(json);
            if (ticker is null)
            {
                _logger.LogWarning("Yahoo Finance returned no ticker for ISIN {Isin}; Yahoo news is skipped.", candidate);
            }

            return ticker;
        }
        catch (Exception ex) when (ex is HttpRequestException or JsonException)
        {
            _logger.LogWarning(ex, "Yahoo Finance ticker lookup failed for ISIN {Isin}.", candidate);
            return null;
        }
    }

    /// <summary>
    /// Prefers an equity or ETF over other instrument types (options, indices...) that the search may also return.
    /// </summary>
    private string? ExtractTicker(string json)
    {
        using var document = JsonDocument.Parse(json);
        if (!document.RootElement.TryGetProperty("quotes", out var quotes) || quotes.ValueKind != JsonValueKind.Array)
        {
            return null;
        }

        string? first = null;
        foreach (var quote in quotes.EnumerateArray())
        {
            // "symbol" is the name of the field in Yahoo's JSON: it holds what we call the ticker.
            if (!quote.TryGetProperty("symbol", out var tickerElement) || tickerElement.GetString() is not { Length: > 0 } ticker)
            {
                continue;
            }

            first ??= ticker;

            var type = quote.TryGetProperty("quoteType", out var typeElement) ? typeElement.GetString() : null;
            if (string.Equals(type, "EQUITY", StringComparison.OrdinalIgnoreCase)
                || string.Equals(type, "ETF", StringComparison.OrdinalIgnoreCase))
            {
                return ticker;
            }
        }

        return first;
    }
}

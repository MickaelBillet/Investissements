
namespace AgentAI;

/// <summary>
/// Retrieves recent news about a listed company.
/// </summary>
public interface INewsService
{
    /// <summary>
    /// Returns the news published during the last <paramref name="days"/> days, most recent first.
    /// </summary>
    /// <param name="company">Company name, searched on Google News.</param>
    /// <param name="isin">
    /// Optional ISIN; when provided and resolvable to a Yahoo Finance ticker, Yahoo Finance is queried as well.
    /// An unusable ISIN only means Yahoo is skipped.
    /// </param>
    /// <param name="days">Size of the look-back window, counted back from today.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <exception cref="HttpRequestException">Every queried feed failed, so "no news" cannot be told apart from "no answer".</exception>
    Task<IReadOnlyList<NewsArticle>> GetLatestNewsAsync(
        string company,
        string? isin = null,
        int days = 7,
        CancellationToken cancellationToken = default);
}

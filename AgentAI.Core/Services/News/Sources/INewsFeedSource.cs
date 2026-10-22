namespace AgentAI;

/// <summary>
/// One origin of company news. Adding a source means implementing this interface and registering it
/// in the container (see <see cref="NewsServiceExtensions.AddNewsServices"/>); <see cref="NewsService"/> needs no change.
/// </summary>
public interface INewsFeedSource
{
    /// <summary>
    /// Human-readable name, used in logs and as the default publisher of its articles.
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Fetches the articles matching <paramref name="request"/>.
    /// </summary>
    /// <returns>
    /// The articles (an empty list means "no news"), or <c>null</c> when this source does not apply to the
    /// request (e.g. no ISIN to look up) and must be ignored. Throws when the source could not be reached,
    /// so that "no news" is never confused with "no answer".
    /// </returns>
    Task<IReadOnlyList<NewsArticle>?> FetchAsync(NewsRequest request, CancellationToken cancellationToken);
}


namespace AgentAI;

/// <summary>
/// A single news item as exposed by an RSS feed: headline-level data only, since feeds do not carry
/// the article body.
/// </summary>
/// <param name="Title">Headline, without the trailing " - Publisher" suffix Google News appends.</param>
/// <param name="Source">Publisher name (or the feed name when the feed does not provide one).</param>
/// <param name="PublishedUtc">Publication date, normalized to UTC so articles from different feeds sort consistently.</param>
/// <param name="Url">Link to the article (a redirect URL for Google News).</param>
public sealed record NewsArticle(string Title, string Source, DateTimeOffset PublishedUtc, string Url);

namespace AgentAI;

/// <summary>
/// Downloads and parses one RSS feed.
/// </summary>
public interface IRssFeedReader
{
    /// <param name="url">Feed address.</param>
    /// <param name="defaultSource">Publisher assigned to items that do not name one.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <exception cref="HttpRequestException">The feed could not be downloaded.</exception>
    /// <exception cref="System.Xml.XmlException">The feed is not well-formed XML.</exception>
    Task<IReadOnlyList<NewsArticle>> ReadAsync(Uri url, string defaultSource, CancellationToken cancellationToken);
}

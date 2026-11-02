using System.Globalization;
using System.Xml;
using System.Xml.Linq;

namespace AgentAI;

/// <summary>
/// Turns the XML of an RSS 2.0 feed into <see cref="NewsArticle"/>s. Pure: no I/O.
/// </summary>
internal static class RssFeedParser
{
    /// <exception cref="XmlException">The content is not well-formed XML.</exception>
    public static IReadOnlyList<NewsArticle> Parse(string xml, string defaultSource)
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

    /// <summary>
    /// Google News appends " - Publisher" to every headline; it is already exposed through
    /// <see cref="NewsArticle.Source"/>, so keeping it would duplicate it in the agent's output.
    /// </summary>
    private static string StripSourceSuffix(string title, string source)
    {
        var suffix = $" - {source}";
        return title.EndsWith(suffix, StringComparison.OrdinalIgnoreCase)
            ? title[..^suffix.Length].TrimEnd()
            : title;
    }
}

using System.Xml;
using AgentAI;
using Xunit;

namespace InvestissementsDashboard.Maui.Tests.News;

public class RssFeedParserTests
{
    private static string Feed(string items) => $"<rss version=\"2.0\"><channel>{items}</channel></rss>";

    [Fact]
    public void Parse_CompleteItem_MapsAllFieldsAndConvertsToUtc()
    {
        var xml = Feed("<item><title>Hello</title><link>https://a/1</link><pubDate>Thu, 08 Oct 2026 12:00:00 +0200</pubDate><source>Les Echos</source></item>");

        var article = Assert.Single(RssFeedParser.Parse(xml, "Feed"));

        Assert.Equal(new NewsArticle("Hello", "Les Echos", new DateTimeOffset(2026, 10, 8, 10, 0, 0, TimeSpan.Zero), "https://a/1"), article);
    }

    [Fact]
    public void Parse_ItemWithoutSource_UsesDefaultSource()
    {
        var xml = Feed("<item><title>Hello</title><link>https://a/1</link><pubDate>Thu, 08 Oct 2026 10:00:00 GMT</pubDate></item>");

        Assert.Equal("Feed", Assert.Single(RssFeedParser.Parse(xml, "Feed")).Source);
    }

    [Fact]
    public void Parse_TitleEndingWithSourceSuffix_StripsIt()
    {
        var xml = Feed("<item><title>Edenred wins - Les Echos</title><link>https://a/1</link><pubDate>Thu, 08 Oct 2026 10:00:00 GMT</pubDate><source>Les Echos</source></item>");

        Assert.Equal("Edenred wins", Assert.Single(RssFeedParser.Parse(xml, "Feed")).Title);
    }

    [Theory]
    [InlineData("<item><link>https://a/1</link><pubDate>Thu, 08 Oct 2026 10:00:00 GMT</pubDate></item>")]
    [InlineData("<item><title>T</title><pubDate>Thu, 08 Oct 2026 10:00:00 GMT</pubDate></item>")]
    [InlineData("<item><title>T</title><link>https://a/1</link><pubDate>not a date</pubDate></item>")]
    public void Parse_ItemMissingTitleLinkOrDate_IsSkipped(string item)
    {
        Assert.Empty(RssFeedParser.Parse(Feed(item), "Feed"));
    }

    [Fact]
    public void Parse_NotXml_Throws()
    {
        Assert.ThrowsAny<XmlException>(() => RssFeedParser.Parse("<html", "Feed"));
    }

    [Fact]
    public void Parse_DocumentWithDtd_Throws()
    {
        var xml = "<!DOCTYPE rss [<!ENTITY x \"y\">]><rss><channel></channel></rss>";

        Assert.ThrowsAny<XmlException>(() => RssFeedParser.Parse(xml, "Feed"));
    }
}

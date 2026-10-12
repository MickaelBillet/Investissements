using AgentAI;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace InvestissementsDashboard.Maui.Tests.News;

public class FeedSourceTests
{
    private static readonly DateTimeOffset Date = new(2026, 10, 8, 10, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task GoogleFetchAsync_ArticleNotNamingCompany_IsDropped()
    {
        var reader = new Mock<IRssFeedReader>();
        reader.Setup(r => r.ReadAsync(It.IsAny<Uri>(), "Google News", It.IsAny<CancellationToken>()))
              .ReturnsAsync([new NewsArticle("Edenred relève ses objectifs", "S", Date, "https://a/1"), new NewsArticle("Airbus en hausse", "S", Date, "https://a/2")]);
        var source = new GoogleNewsFeedSource(reader.Object, NullLogger<GoogleNewsFeedSource>.Instance);

        var result = await source.FetchAsync(new NewsRequest("Edenred", null, 7), default);

        Assert.Equal(["Edenred relève ses objectifs"], result!.Select(a => a.Title));
    }

    [Fact]
    public async Task GoogleFetchAsync_Request_BuildsExactPhraseQueryWithWindow()
    {
        Uri? requested = null;
        var reader = new Mock<IRssFeedReader>();
        reader.Setup(r => r.ReadAsync(It.IsAny<Uri>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
              .Callback<Uri, string, CancellationToken>((uri, _, _) => requested = uri)
              .ReturnsAsync([]);
        var source = new GoogleNewsFeedSource(reader.Object, NullLogger<GoogleNewsFeedSource>.Instance);

        await source.FetchAsync(new NewsRequest("Air Liquide", null, 7), default);

        Assert.Equal("news.google.com", requested!.Host);
        Assert.Contains(Uri.EscapeDataString("\"Air Liquide\" when:7d"), requested.Query);
    }

    [Fact]
    public async Task YahooFetchAsync_TickerResolved_ReadsFeedWithoutTitleFiltering()
    {
        var resolver = Resolver("""{"quotes":[{"symbol":"AI.PA","quoteType":"EQUITY"}]}""");
        var reader = new Mock<IRssFeedReader>();
        reader.Setup(r => r.ReadAsync(It.Is<Uri>(u => u.Host == "feeds.finance.yahoo.com" && u.Query.Contains("s=AI.PA&")), "Yahoo Finance", It.IsAny<CancellationToken>()))
              .ReturnsAsync([new NewsArticle("Euro Stoxx 50 closes higher", "Yahoo Finance", Date, "https://b/1")]);
        var source = new YahooFinanceFeedSource(resolver, reader.Object);

        var result = await source.FetchAsync(new NewsRequest("Air Liquide", "FR0000120073", 7), default);

        Assert.Single(result!);
    }

    [Fact]
    public async Task YahooFetchAsync_NoTicker_ReturnsNullWithoutReadingFeed()
    {
        var resolver = Resolver("""{"quotes":[]}""");
        var reader = new Mock<IRssFeedReader>(MockBehavior.Strict);
        var source = new YahooFinanceFeedSource(resolver, reader.Object);

        var result = await source.FetchAsync(new NewsRequest("Air Liquide", "FR0000120073", 7), default);

        Assert.Null(result);
    }

    [Fact]
    public async Task YahooFetchAsync_FeedFails_Throws()
    {
        var resolver = Resolver("""{"quotes":[{"symbol":"AI.PA","quoteType":"EQUITY"}]}""");
        var reader = new Mock<IRssFeedReader>();
        reader.Setup(r => r.ReadAsync(It.IsAny<Uri>(), It.IsAny<string>(), It.IsAny<CancellationToken>())).ThrowsAsync(new HttpRequestException());
        var source = new YahooFinanceFeedSource(resolver, reader.Object);

        await Assert.ThrowsAsync<HttpRequestException>(() => source.FetchAsync(new NewsRequest("Air Liquide", "FR0000120073", 7), default));
    }

    // The resolver is a concrete class, so it is exercised through a stubbed HTTP pipeline rather than mocked.
    private static YahooTickerResolver Resolver(string searchJson) =>
        new(new HttpClient(new StubHandler(searchJson)), NullLogger<YahooTickerResolver>.Instance);

    private sealed class StubHandler(string json) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
            Task.FromResult(new HttpResponseMessage(System.Net.HttpStatusCode.OK) { Content = new StringContent(json) });
    }
}

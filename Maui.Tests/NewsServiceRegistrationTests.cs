using System.Net;
using System.Text;
using AgentAI;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace InvestissementsDashboard.Maui.Tests;

/// <summary>
/// End-to-end tests of the container registration: the real graph of sources, reader and resolver
/// behind a stubbed HTTP pipeline.
/// </summary>
public class NewsServiceRegistrationTests
{
    private static readonly DateTimeOffset Now = new(2026, 10, 9, 12, 0, 0, TimeSpan.Zero);
    private const string RecentDate = "Thu, 08 Oct 2026 10:00:00 GMT";
    private const string WoltersKluwerIsin = "NL0000395903";

    [Fact]
    public async Task GetLatestNewsAsync_TitleWithoutCompany_IsDropped()
    {
        var service = Create(google: Rss(
            ("Wolters Kluwer publie ses résultats", "https://a.test/1"),
            ("Avis d'analystes : évolutions pour Airbus, Thales, Saint-Gobain...", "https://a.test/2")));

        var result = await service.GetLatestNewsAsync("Wolters Kluwer");

        Assert.Equal(["Wolters Kluwer publie ses résultats"], result.Select(a => a.Title));
    }

    [Fact]
    public async Task GetLatestNewsAsync_TitleWithCompany_IsKept()
    {
        var service = Create(google: Rss(("Edenred relève ses objectifs", "https://a.test/1")));

        var result = await service.GetLatestNewsAsync("Edenred");

        Assert.Single(result);
    }

    [Fact]
    public async Task GetLatestNewsAsync_AccentsAndCase_StillMatch()
    {
        var service = Create(google: Rss(("EDENRED : nouveau contrat", "https://a.test/1"), ("Électricité de France en hausse", "https://a.test/2")));

        var edenred = await service.GetLatestNewsAsync("Édenred");
        var edf = await service.GetLatestNewsAsync("electricite de france");

        Assert.Single(edenred);
        Assert.Single(edf);
    }

    [Fact]
    public async Task GetLatestNewsAsync_LegalSuffixInCompanyName_IsIgnored()
    {
        var service = Create(google: Rss(("Wolters Kluwer gagne un contrat", "https://a.test/1")));

        var result = await service.GetLatestNewsAsync("Wolters Kluwer NV");

        Assert.Single(result);
    }

    [Fact]
    public async Task GetLatestNewsAsync_PartialWord_DoesNotMatch()
    {
        var service = Create(google: Rss(("Farm equipment sales rise", "https://a.test/1"), ("ARM annonce un nouveau processeur", "https://a.test/2")));

        var result = await service.GetLatestNewsAsync("ARM");

        Assert.Equal(["ARM annonce un nouveau processeur"], result.Select(a => a.Title));
    }

    [Fact]
    public async Task GetLatestNewsAsync_WithIsin_YahooArticlesAreNotFiltered()
    {
        var service = Create(
            google: Rss(("Wolters Kluwer publie ses résultats", "https://a.test/1")),
            yahoo: Rss(("Euro Stoxx 50 closes higher", "https://b.test/1")),
            search: Search(("WKL.AS", "EQUITY")));

        var result = await service.GetLatestNewsAsync("Wolters Kluwer", isin: WoltersKluwerIsin);

        Assert.Equal(2, result.Count);
    }

    [Fact]
    public async Task GetLatestNewsAsync_WithIsin_QueriesYahooFeedWithResolvedTicker()
    {
        var requests = new List<Uri>();
        var service = Create(
            google: Rss(("Wolters Kluwer publie ses résultats", "https://a.test/1")),
            yahoo: Rss(("Euro Stoxx 50 closes higher", "https://b.test/1")),
            search: Search(("WKL.AS.OPT", "OPTION"), ("WKL.AS", "EQUITY")),
            requests: requests);

        await service.GetLatestNewsAsync("Wolters Kluwer", isin: WoltersKluwerIsin);

        Assert.Contains(requests, uri => uri.Host == "feeds.finance.yahoo.com" && uri.Query.Contains("s=WKL.AS&"));
        Assert.Contains(requests, uri => uri.Host == "query1.finance.yahoo.com" && uri.Query.Contains($"q={WoltersKluwerIsin}"));
    }

    [Fact]
    public async Task GetLatestNewsAsync_TickerLookupFails_FallsBackToGoogleOnly()
    {
        var service = Create(
            google: Rss(("Wolters Kluwer publie ses résultats", "https://a.test/1")),
            yahoo: Rss(("Euro Stoxx 50 closes higher", "https://b.test/1")),
            search: null);

        var result = await service.GetLatestNewsAsync("Wolters Kluwer", isin: WoltersKluwerIsin);

        Assert.Equal(["Wolters Kluwer publie ses résultats"], result.Select(a => a.Title));
    }

    [Fact]
    public async Task GetLatestNewsAsync_TickerLookupFindsNothing_FallsBackToGoogleOnly()
    {
        var requests = new List<Uri>();
        var service = Create(
            google: Rss(("Wolters Kluwer publie ses résultats", "https://a.test/1")),
            yahoo: Rss(("Euro Stoxx 50 closes higher", "https://b.test/1")),
            search: "{\"quotes\":[]}",
            requests: requests);

        var result = await service.GetLatestNewsAsync("Wolters Kluwer", isin: WoltersKluwerIsin);

        Assert.Single(result);
        Assert.DoesNotContain(requests, uri => uri.Host == "feeds.finance.yahoo.com");
    }

    [Theory]
    [InlineData("WKL.AS")]
    [InlineData("not an isin")]
    [InlineData("   ")]
    public async Task GetLatestNewsAsync_InvalidIsin_SkipsYahooWithoutLookup(string isin)
    {
        var requests = new List<Uri>();
        var service = Create(
            google: Rss(("Wolters Kluwer publie ses résultats", "https://a.test/1")),
            yahoo: Rss(("Euro Stoxx 50 closes higher", "https://b.test/1")),
            search: Search(("WKL.AS", "EQUITY")),
            requests: requests);

        var result = await service.GetLatestNewsAsync("Wolters Kluwer", isin: isin);

        Assert.Single(result);
        Assert.DoesNotContain(requests, uri => uri.Host.Contains("yahoo"));
    }

    [Fact]
    public async Task GetLatestNewsAsync_NoTitleMatches_ReturnsEmptyList()
    {
        var service = Create(google: Rss(("Airbus et Thales en hausse", "https://a.test/1")));

        var result = await service.GetLatestNewsAsync("Wolters Kluwer");

        Assert.Empty(result);
    }

    [Fact]
    public async Task GetLatestNewsAsync_AllFeedsFail_Throws()
    {
        var service = Create(google: null);

        await Assert.ThrowsAsync<HttpRequestException>(() => service.GetLatestNewsAsync("Wolters Kluwer"));
    }

    [Fact]
    public async Task AddNewsServices_HostRegistersExtraSource_ItsArticlesAreIncluded()
    {
        var extra = new Mock<INewsFeedSource>();
        extra.SetupGet(s => s.Name).Returns("Extra");
        extra.Setup(s => s.FetchAsync(It.IsAny<NewsRequest>(), It.IsAny<CancellationToken>()))
             .ReturnsAsync([new NewsArticle("Wolters Kluwer from a host source", "Extra", Now.AddDays(-1), "https://c.test/1")]);
        using var provider = BuildProvider(
            services => services.AddSingleton(extra.Object),
            google: Rss(("Wolters Kluwer publie ses résultats", "https://a.test/1")));

        var result = await provider.GetRequiredService<INewsService>().GetLatestNewsAsync("Wolters Kluwer");

        Assert.Equal(2, result.Count);
    }

    [Fact]
    public void AddNewsServices_HostRegisteredNewsService_IsNotReplaced()
    {
        var custom = new Mock<INewsService>().Object;
        using var provider = BuildProvider(services => services.AddSingleton(custom), google: null);

        Assert.Same(custom, provider.GetRequiredService<INewsService>());
    }

    [Fact]
    public async Task AddInvestZaptoMcp_Request_IsBufferedWithContentLength()
    {
        long? contentLength = null;
        var services = new ServiceCollection();
        services.AddInvestZaptoMcp().ConfigurePrimaryHttpMessageHandler(() => new StubHandler(request =>
        {
            contentLength = request.Content?.Headers.ContentLength;
            return new HttpResponseMessage(HttpStatusCode.OK);
        }));
        using var provider = services.BuildServiceProvider();
        var client = provider.GetRequiredService<IHttpClientFactory>().CreateClient(nameof(InvestZaptoMcpClient));

        using var content = new UnknownLengthContent("{\"jsonrpc\":\"2.0\"}");
        await client.PostAsync("https://invest.test/api/mcp", content);

        Assert.Equal("{\"jsonrpc\":\"2.0\"}".Length, contentLength);
    }

    [Fact]
    public void AddInvestZaptoMcp_Resolved_ReturnsTypedClient()
    {
        var services = new ServiceCollection();
        services.AddSingleton<ILoggerFactory>(NullLoggerFactory.Instance);
        services.AddInvestZaptoMcp();
        using var provider = services.BuildServiceProvider();

        Assert.NotNull(provider.GetRequiredService<InvestZaptoMcpClient>());
    }

    private static INewsService Create(string? google, string? yahoo = null, string? search = null, List<Uri>? requests = null) =>
        BuildProvider(services => { }, google, yahoo, search, requests).GetRequiredService<INewsService>();

    private static ServiceProvider BuildProvider(
        Action<IServiceCollection> configure, string? google, string? yahoo = null, string? search = null, List<Uri>? requests = null)
    {
        var services = new ServiceCollection();
        services.AddSingleton<TimeProvider>(new FixedTimeProvider(Now));
        services.AddSingleton<ILoggerFactory>(NullLoggerFactory.Instance);
        services.AddSingleton(typeof(ILogger<>), typeof(NullLogger<>));
        configure(services);

        services.AddNewsServices();

        // Every client of the container gets the stubbed primary handler: the registered pipelines stay real.
        services.ConfigureHttpClientDefaults(builder => builder.ConfigurePrimaryHttpMessageHandler(() => new StubHandler(request =>
        {
            requests?.Add(request.RequestUri!);
            var host = request.RequestUri!.Host;
            var body = host.StartsWith("query1.", StringComparison.Ordinal) ? search
                : host.Contains("yahoo") ? yahoo
                : google;
            return body is null
                ? new HttpResponseMessage(HttpStatusCode.InternalServerError)
                : new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(body, Encoding.UTF8, "application/rss+xml") };
        })));

        return services.BuildServiceProvider();
    }

    private static string Search(params (string Ticker, string QuoteType)[] quotes) =>
        "{\"quotes\":[" + string.Join(",", quotes.Select(q => $"{{\"symbol\":\"{q.Ticker}\",\"quoteType\":\"{q.QuoteType}\"}}")) + "]}";

    private static string Rss(params (string Title, string Link)[] items) =>
        "<rss version=\"2.0\"><channel>"
        + string.Concat(items.Select(i =>
            $"<item><title>{System.Security.SecurityElement.Escape(i.Title)}</title><link>{i.Link}</link><pubDate>{RecentDate}</pubDate></item>"))
        + "</channel></rss>";

    // A body whose length is not known up front, as the MCP SDK's request content.
    private sealed class UnknownLengthContent(string text) : HttpContent
    {
        protected override Task SerializeToStreamAsync(Stream stream, TransportContext? context) =>
            stream.WriteAsync(Encoding.UTF8.GetBytes(text)).AsTask();

        protected override bool TryComputeLength(out long length)
        {
            length = 0;
            return false;
        }
    }

    private sealed class StubHandler(Func<HttpRequestMessage, HttpResponseMessage> respond) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
            Task.FromResult(respond(request));
    }

    private sealed class FixedTimeProvider(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;
    }
}

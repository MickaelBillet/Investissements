using System.Net;
using System.Text;
using AgentAI;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace InvestissementsDashboard.Maui.Tests;

public class RssNewsServiceTests
{
    private static readonly DateTimeOffset Now = new(2026, 10, 9, 12, 0, 0, TimeSpan.Zero);
    private const string RecentDate = "Thu, 08 Oct 2026 10:00:00 GMT";

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
    public async Task GetLatestNewsAsync_WithTicker_YahooArticlesAreNotFiltered()
    {
        var service = Create(
            google: Rss(("Wolters Kluwer publie ses résultats", "https://a.test/1")),
            yahoo: Rss(("Euro Stoxx 50 closes higher", "https://b.test/1")));

        var result = await service.GetLatestNewsAsync("Wolters Kluwer", ticker: "WKL.AS");

        Assert.Equal(2, result.Count);
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

    private static RssNewsService Create(string? google, string? yahoo = null) =>
        new(new HttpClient(new StubHandler(request =>
            {
                var body = request.RequestUri!.Host.Contains("yahoo") ? yahoo : google;
                return body is null
                    ? new HttpResponseMessage(HttpStatusCode.InternalServerError)
                    : new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(body, Encoding.UTF8, "application/rss+xml") };
            })),
            new FixedTimeProvider(Now),
            NullLogger<RssNewsService>.Instance);

    private static string Rss(params (string Title, string Link)[] items) =>
        "<rss version=\"2.0\"><channel>"
        + string.Concat(items.Select(i =>
            $"<item><title>{System.Security.SecurityElement.Escape(i.Title)}</title><link>{i.Link}</link><pubDate>{RecentDate}</pubDate></item>"))
        + "</channel></rss>";

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

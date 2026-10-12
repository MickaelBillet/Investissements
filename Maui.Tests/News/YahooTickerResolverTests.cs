using System.Net;
using System.Text;
using AgentAI;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace InvestissementsDashboard.Maui.Tests.News;

public class YahooTickerResolverTests
{
    private const string Isin = "FR0000120073";

    private static YahooTickerResolver Create(Func<HttpRequestMessage, HttpResponseMessage> respond, List<Uri>? requests = null) =>
        new(new HttpClient(new StubHandler(request =>
            {
                requests?.Add(request.RequestUri!);
                return respond(request);
            })),
            NullLogger<YahooTickerResolver>.Instance);

    private static HttpResponseMessage Json(string body) =>
        new(HttpStatusCode.OK) { Content = new StringContent(body, Encoding.UTF8, "application/json") };

    [Fact]
    public async Task ResolveAsync_EquityAmongResults_ReturnsEquityTicker()
    {
        var resolver = Create(_ => Json("""{"quotes":[{"symbol":"AI.OPT","quoteType":"OPTION"},{"symbol":"AI.PA","quoteType":"EQUITY"}]}"""));

        Assert.Equal("AI.PA", await resolver.ResolveAsync(Isin, default));
    }

    [Fact]
    public async Task ResolveAsync_NoEquityOrEtf_ReturnsFirstTicker()
    {
        var resolver = Create(_ => Json("""{"quotes":[{"symbol":"CAC.IDX","quoteType":"INDEX"}]}"""));

        Assert.Equal("CAC.IDX", await resolver.ResolveAsync(Isin, default));
    }

    [Fact]
    public async Task ResolveAsync_LowercaseIsinWithSpaces_IsNormalizedBeforeLookup()
    {
        var requests = new List<Uri>();
        var resolver = Create(_ => Json("""{"quotes":[{"symbol":"AI.PA","quoteType":"EQUITY"}]}"""), requests);

        await resolver.ResolveAsync($" {Isin.ToLowerInvariant()} ", default);

        Assert.Contains($"q={Isin}", Assert.Single(requests).Query);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("  ")]
    [InlineData("AI.PA")]
    [InlineData("FR0000120073 OR 1=1")]
    public async Task ResolveAsync_BlankOrMalformedIsin_ReturnsNullWithoutRequest(string? isin)
    {
        var requests = new List<Uri>();
        var resolver = Create(_ => Json("{}"), requests);

        Assert.Null(await resolver.ResolveAsync(isin, default));
        Assert.Empty(requests);
    }

    [Theory]
    [InlineData("""{"quotes":[]}""")]
    [InlineData("""{"other":1}""")]
    public async Task ResolveAsync_NoQuotes_ReturnsNull(string body)
    {
        var resolver = Create(_ => Json(body));

        Assert.Null(await resolver.ResolveAsync(Isin, default));
    }

    [Fact]
    public async Task ResolveAsync_HttpError_ReturnsNull()
    {
        var resolver = Create(_ => new HttpResponseMessage(HttpStatusCode.TooManyRequests));

        Assert.Null(await resolver.ResolveAsync(Isin, default));
    }

    [Fact]
    public async Task ResolveAsync_InvalidJson_ReturnsNull()
    {
        var resolver = Create(_ => Json("<html>not json</html>"));

        Assert.Null(await resolver.ResolveAsync(Isin, default));
    }

    private sealed class StubHandler(Func<HttpRequestMessage, HttpResponseMessage> respond) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
            Task.FromResult(respond(request));
    }
}

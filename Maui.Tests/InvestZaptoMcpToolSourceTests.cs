using System.Net;
using AgentAI;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace InvestissementsDashboard.Maui.Tests;

public class InvestZaptoMcpToolSourceTests
{
    private const string Endpoint = "https://invest.test/api/mcp";

    private int _requestCount;

    [Fact]
    public async Task GetToolsAsync_NoEndpoint_ThrowsWithoutContactingServer()
    {
        await using var source = new InvestZaptoMcpToolSource(CreateConnector(HttpStatusCode.Unauthorized), endpoint: null);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => source.GetToolsAsync());

        Assert.Contains(nameof(AgentAIOptions.InvestZaptoMcpUrl), ex.Message);
        Assert.Equal(0, _requestCount);
    }

    [Fact]
    public async Task GetToolsAsync_ConnectionRefused_PropagatesAndRetriesOnNextCall()
    {
        await using var source = new InvestZaptoMcpToolSource(CreateConnector(HttpStatusCode.Unauthorized), Endpoint);

        var first = await Assert.ThrowsAsync<HttpRequestException>(() => source.GetToolsAsync());
        var requestsAfterFirstCall = _requestCount;
        await Assert.ThrowsAsync<HttpRequestException>(() => source.GetToolsAsync());

        Assert.Equal(HttpStatusCode.Unauthorized, first.StatusCode);
        Assert.True(requestsAfterFirstCall > 0);
        Assert.True(_requestCount > requestsAfterFirstCall);
    }

    [Fact]
    public async Task DisposeAsync_NeverConnected_DoesNotThrow()
    {
        var source = new InvestZaptoMcpToolSource(CreateConnector(HttpStatusCode.Unauthorized), Endpoint);

        await source.DisposeAsync();

        Assert.Equal(0, _requestCount);
    }

    private InvestZaptoMcpClient CreateConnector(HttpStatusCode status) =>
        new(new HttpClient(new StubHandler(() =>
        {
            Interlocked.Increment(ref _requestCount);
            return new HttpResponseMessage(status);
        })), NullLoggerFactory.Instance);

    private sealed class StubHandler(Func<HttpResponseMessage> respond) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
            Task.FromResult(respond());
    }
}

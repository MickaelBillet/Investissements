using InvestissementsDashboard.Client.Services;
using InvestissementsDashboard.Maui.Services;
using Moq;

namespace InvestissementsDashboard.Maui.Tests;

public class McpApiKeyHandlerTests
{
    private readonly Mock<IKeyValueStore> _store = new();

    private sealed class CapturingHandler : HttpMessageHandler
    {
        public HttpRequestMessage? Request { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Request = request;
            return Task.FromResult(new HttpResponseMessage());
        }
    }

    private async Task<HttpRequestMessage> SendAsync(HttpRequestMessage request)
    {
        var inner = new CapturingHandler();
        using var client = new HttpClient(new McpApiKeyHandler(_store.Object) { InnerHandler = inner });
        await client.SendAsync(request);
        return inner.Request!;
    }

    [Fact]
    public async Task SendAsync_WithStoredKey_AddsHeader()
    {
        _store.Setup(s => s.GetAsync(AgentSecretKeys.McpApiKey)).ReturnsAsync("secret");

        var sent = await SendAsync(new HttpRequestMessage(HttpMethod.Post, "https://invest.test/api/mcp"));

        Assert.Equal("secret", Assert.Single(sent.Headers.GetValues(McpApiKeyHandler.HeaderName)));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task SendAsync_WithoutStoredKey_AddsNoHeader(string? stored)
    {
        _store.Setup(s => s.GetAsync(AgentSecretKeys.McpApiKey)).ReturnsAsync(stored);

        var sent = await SendAsync(new HttpRequestMessage(HttpMethod.Post, "https://invest.test/api/mcp"));

        Assert.False(sent.Headers.Contains(McpApiKeyHandler.HeaderName));
    }

    [Fact]
    public async Task SendAsync_WhenHeaderAlreadySet_KeepsIt()
    {
        _store.Setup(s => s.GetAsync(AgentSecretKeys.McpApiKey)).ReturnsAsync("stored");
        var request = new HttpRequestMessage(HttpMethod.Post, "https://invest.test/api/mcp");
        request.Headers.Add(McpApiKeyHandler.HeaderName, "explicit");

        var sent = await SendAsync(request);

        Assert.Equal("explicit", Assert.Single(sent.Headers.GetValues(McpApiKeyHandler.HeaderName)));
    }
}

using System.Net;
using System.Text;
using InvestissementsDashboard.Maui.Services;
using InvestissementsDashboard.Shared.Models;
using Xunit;

namespace InvestissementsDashboard.Maui.Tests;

public class ApiAgentConfigSourceTests
{
    private HttpRequestMessage? _request;

    private ApiAgentConfigSource Create(HttpResponseMessage response) =>
        new(new HttpClient(new StubHandler(request =>
            {
                _request = request;
                return response;
            }))
            { BaseAddress = new Uri("https://invest.test/") });

    [Fact]
    public async Task GetAsync_ApiAnswers_ReturnsConfigFromConfigRoute()
    {
        var source = Create(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("""{"foundryEndpoint":"https://foundry.test/","model":null}""", Encoding.UTF8, "application/json"),
        });

        var config = await source.GetAsync(default);

        Assert.Equal(HttpMethod.Get, _request!.Method);
        Assert.Equal(new Uri("https://invest.test/api/agents/config"), _request.RequestUri);
        Assert.Equal(new AgentConfigDto("https://foundry.test/", null), config);
    }

    [Fact]
    public async Task GetAsync_ApiRefuses_ThrowsWithStatus()
    {
        var source = Create(new HttpResponseMessage(HttpStatusCode.Unauthorized));

        var ex = await Assert.ThrowsAsync<HttpRequestException>(() => source.GetAsync(default));

        Assert.Equal(HttpStatusCode.Unauthorized, ex.StatusCode);
    }

    private sealed class StubHandler(Func<HttpRequestMessage, HttpResponseMessage> respond) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
            Task.FromResult(respond(request));
    }
}

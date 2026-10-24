using System.Net;
using System.Text;
using System.Text.Json;
using InvestissementsDashboard.Maui.Services;
using InvestissementsDashboard.Shared.Models;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace InvestissementsDashboard.Maui.Tests.WebSearch;

public class ApiWebSearchServiceTests
{
    private HttpRequestMessage? _request;
    private string? _requestBody;

    private ApiWebSearchService Create(Func<HttpResponseMessage> respond) =>
        new(new HttpClient(new StubHandler(async request =>
            {
                _request = request;
                _requestBody = await request.Content!.ReadAsStringAsync();
                return respond();
            }))
            { BaseAddress = new Uri("https://invest.test/") },
            NullLogger<ApiWebSearchService>.Instance);

    private static HttpResponseMessage Json(string body) =>
        new(HttpStatusCode.OK) { Content = new StringContent(body, Encoding.UTF8, "application/json") };

    [Fact]
    public async Task SearchAsync_Request_PostsToApiWithEnumNames()
    {
        var service = Create(() => Json("[]"));

        await service.SearchAsync("taux OAT 10 ans", WebSearchTopic.Finance, WebSearchTimeRange.Week, default);

        Assert.Equal(HttpMethod.Post, _request!.Method);
        Assert.Equal(new Uri("https://invest.test/api/websearch"), _request.RequestUri);
        using var body = JsonDocument.Parse(_requestBody!);
        Assert.Equal("taux OAT 10 ans", body.RootElement.GetProperty("query").GetString());
        Assert.Equal("Finance", body.RootElement.GetProperty("topic").GetString());
        Assert.Equal("Week", body.RootElement.GetProperty("timeRange").GetString());
    }

    [Fact]
    public async Task SearchAsync_ApiAnswers_ReturnsResults()
    {
        var service = Create(() => Json("""[{"title":"Titre","url":"https://news.test/1","content":"Extrait","publishedDate":null}]"""));

        var results = await service.SearchAsync("q", WebSearchTopic.General, WebSearchTimeRange.Any, default);

        Assert.Equal(new WebSearchResultDto("Titre", "https://news.test/1", "Extrait", null), Assert.Single(results));
    }

    [Theory]
    [InlineData(HttpStatusCode.Unauthorized)]
    [InlineData(HttpStatusCode.BadGateway)]
    public async Task SearchAsync_ApiFails_ThrowsHttpRequestException(HttpStatusCode status)
    {
        var service = Create(() => new HttpResponseMessage(status));

        var ex = await Assert.ThrowsAsync<HttpRequestException>(() => service.SearchAsync("q", WebSearchTopic.General, WebSearchTimeRange.Any, default));

        Assert.Equal(status, ex.StatusCode);
    }

    private sealed class StubHandler(Func<HttpRequestMessage, Task<HttpResponseMessage>> respond) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) => respond(request);
    }
}

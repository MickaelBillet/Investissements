using System.Net;
using System.Text;
using System.Text.Json;
using InvestissementsDashboard.Api.Services;
using InvestissementsDashboard.Shared.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace InvestissementsDashboard.Api.Tests.Services;

public class WebSearchServiceTests
{
    private HttpRequestMessage? _request;
    private string? _requestBody;

    private WebSearchService CreateService(Func<HttpResponseMessage> respond, string? key = "tvly-secret")
    {
        var httpClient = new HttpClient(new StubHandler(async request =>
        {
            _request = request;
            _requestBody = await request.Content!.ReadAsStringAsync();
            return respond();
        }));

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["WEB_SEARCH_API_KEY"] = key })
            .Build();

        return new WebSearchService(httpClient, configuration, NullLogger<WebSearchService>.Instance);
    }

    private static HttpResponseMessage Json(string body) =>
        new(HttpStatusCode.OK) { Content = new StringContent(body, Encoding.UTF8, "application/json") };

    [Fact]
    public async Task SearchAsync_Request_PostsQueryWithOneCreditOptionsAndBearerKey()
    {
        var service = CreateService(() => Json("""{"results":[]}"""));

        await service.SearchAsync(new WebSearchRequestDto("taux directeur BCE", WebSearchTopic.Finance, WebSearchTimeRange.Month));

        Assert.Equal(HttpMethod.Post, _request!.Method);
        Assert.Equal(WebSearchService.Endpoint, _request.RequestUri);
        Assert.Equal("Bearer", _request.Headers.Authorization!.Scheme);
        Assert.Equal("tvly-secret", _request.Headers.Authorization.Parameter);
        using var body = JsonDocument.Parse(_requestBody!);
        var root = body.RootElement;
        Assert.Equal("taux directeur BCE", root.GetProperty("query").GetString());
        Assert.Equal("finance", root.GetProperty("topic").GetString());
        Assert.Equal("basic", root.GetProperty("search_depth").GetString());
        Assert.Equal(WebSearchService.MaxResults, root.GetProperty("max_results").GetInt32());
        Assert.Equal("month", root.GetProperty("time_range").GetString());
        Assert.True(root.GetProperty("include_published_date").GetBoolean());
    }

    [Fact]
    public async Task SearchAsync_AnyTimeRange_SendsNoTimeRange()
    {
        var service = CreateService(() => Json("""{"results":[]}"""));

        await service.SearchAsync(new WebSearchRequestDto("prix du Brent"));

        using var body = JsonDocument.Parse(_requestBody!);
        Assert.False(body.RootElement.TryGetProperty("time_range", out _));
    }

    [Fact]
    public async Task SearchAsync_Results_MapsTitleUrlContentAndDate()
    {
        var service = CreateService(() => Json("""
            {"query":"q","results":[
              {"title":"BCE : taux inchangés","url":"https://ecb.test/1","content":"La BCE maintient...","score":0.9,"published_date":"2026-10-16"},
              {"title":"Sans date","url":"https://news.test/2","content":"Extrait","score":0.5,"published_date":null}
            ]}
            """));

        var results = await service.SearchAsync(new WebSearchRequestDto("q", WebSearchTopic.News, WebSearchTimeRange.Week));

        Assert.Equal(2, results.Count);
        Assert.Equal(new WebSearchResultDto("BCE : taux inchangés", "https://ecb.test/1", "La BCE maintient...", "2026-10-16"), results[0]);
        Assert.Null(results[1].PublishedDate);
    }

    [Fact]
    public async Task SearchAsync_NoResultsField_ReturnsEmpty()
    {
        var service = CreateService(() => Json("""{"query":"q"}"""));

        Assert.Empty(await service.SearchAsync(new WebSearchRequestDto("q")));
    }

    [Theory]
    [InlineData(HttpStatusCode.Unauthorized)]
    [InlineData((HttpStatusCode)432)]
    public async Task SearchAsync_ProviderRefuses_ThrowsHttpRequestExceptionWithStatus(HttpStatusCode status)
    {
        var service = CreateService(() => new HttpResponseMessage(status));

        var ex = await Assert.ThrowsAsync<HttpRequestException>(() => service.SearchAsync(new WebSearchRequestDto("q")));

        Assert.Equal(status, ex.StatusCode);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public async Task SearchAsync_KeyNotConfigured_ThrowsWithoutRequest(string? key)
    {
        var service = CreateService(() => Json("""{"results":[]}"""), key);

        await Assert.ThrowsAsync<InvalidOperationException>(() => service.SearchAsync(new WebSearchRequestDto("q")));
        Assert.Null(_request);
    }

    private sealed class StubHandler(Func<HttpRequestMessage, Task<HttpResponseMessage>> respond) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) => respond(request);
    }
}

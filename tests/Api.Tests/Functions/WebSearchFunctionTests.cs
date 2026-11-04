using System.Text;
using InvestissementsDashboard.Api.Functions;
using InvestissementsDashboard.Api.Services;
using InvestissementsDashboard.Shared.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace InvestissementsDashboard.Api.Tests.Functions;

public class WebSearchFunctionTests
{
    private readonly Mock<IWebSearchService> _service = new();

    private WebSearchFunction CreateFunction() => new(_service.Object, NullLogger<WebSearchFunction>.Instance);

    private static HttpRequest MockRequest(string body)
    {
        var request = new Mock<HttpRequest>();
        request.Setup(r => r.Body).Returns(new MemoryStream(Encoding.UTF8.GetBytes(body)));
        return request.Object;
    }

    [Theory]
    [InlineData("""{"query":"","topic":"News","timeRange":"Week"}""")]
    [InlineData("""{"topic":"News"}""")]
    [InlineData("null")]
    [InlineData("not json")]
    [InlineData("""{"query":"q","topic":"Sports"}""")]
    public async Task Search_InvalidBody_ReturnsBadRequest(string body)
    {
        var result = await CreateFunction().Search(MockRequest(body), CancellationToken.None);

        Assert.IsType<BadRequestObjectResult>(result);
        _service.Verify(s => s.SearchAsync(It.IsAny<WebSearchRequestDto>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Search_ValidBody_ReturnsOkWithResults()
    {
        WebSearchResultDto[] results = [new("Titre", "https://news.test/1", "Extrait", "2026-10-20")];
        _service
            .Setup(s => s.SearchAsync(new WebSearchRequestDto("inflation zone euro", WebSearchTopic.News, WebSearchTimeRange.Month), It.IsAny<CancellationToken>()))
            .ReturnsAsync(results);

        var result = await CreateFunction().Search(MockRequest("""{"query":"inflation zone euro","topic":"News","timeRange":"Month"}"""), CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.Same(results, ok.Value);
    }

    [Fact]
    public async Task Search_WhenProviderFails_ReturnsBadGateway()
    {
        _service
            .Setup(s => s.SearchAsync(It.IsAny<WebSearchRequestDto>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new HttpRequestException("quota"));

        var result = await CreateFunction().Search(MockRequest("""{"query":"q"}"""), CancellationToken.None);

        var status = Assert.IsType<StatusCodeResult>(result);
        Assert.Equal(StatusCodes.Status502BadGateway, status.StatusCode);
    }

    [Fact]
    public async Task Search_WhenKeyNotConfigured_ReturnsInternalServerError()
    {
        _service
            .Setup(s => s.SearchAsync(It.IsAny<WebSearchRequestDto>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("WEB_SEARCH_API_KEY is not configured."));

        var result = await CreateFunction().Search(MockRequest("""{"query":"q"}"""), CancellationToken.None);

        var status = Assert.IsType<StatusCodeResult>(result);
        Assert.Equal(StatusCodes.Status500InternalServerError, status.StatusCode);
    }
}

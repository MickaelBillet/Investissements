using AgentAI;
using InvestissementsDashboard.Shared.Models;
using Moq;
using Xunit;

namespace InvestissementsDashboard.Maui.Tests.WebSearch;

public class WebSearchToolTests
{
    private readonly Mock<IWebSearchService> _service = new();
    private readonly List<WebSearchResultDto> _results = [new("Titre", "https://news.test/1", "Extrait", "2026-10-20")];

    public WebSearchToolTests()
    {
        _service
            .Setup(s => s.SearchAsync(It.IsAny<string>(), It.IsAny<WebSearchTopic>(), It.IsAny<WebSearchTimeRange>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(_results);
    }

    [Fact]
    public async Task SearchAsync_UnderCap_ForwardsToServiceAndReturnsResults()
    {
        var tool = new WebSearchTool(_service.Object, maxSearches: 2);

        var result = await tool.SearchAsync("inflation zone euro", WebSearchTopic.News, WebSearchTimeRange.Month, default);

        Assert.Same(_results, result);
        _service.Verify(s => s.SearchAsync("inflation zone euro", WebSearchTopic.News, WebSearchTimeRange.Month, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task SearchAsync_BeyondCap_ReturnsLimitMessageWithoutCallingService()
    {
        var tool = new WebSearchTool(_service.Object, maxSearches: 2);
        await tool.SearchAsync("a", WebSearchTopic.General, WebSearchTimeRange.Any, default);
        await tool.SearchAsync("b", WebSearchTopic.General, WebSearchTimeRange.Any, default);

        var result = await tool.SearchAsync("c", WebSearchTopic.General, WebSearchTimeRange.Any, default);

        Assert.Equal(WebSearchTool.LimitReachedMessage, result);
        _service.Verify(s => s.SearchAsync(It.IsAny<string>(), It.IsAny<WebSearchTopic>(), It.IsAny<WebSearchTimeRange>(), It.IsAny<CancellationToken>()), Times.Exactly(2));
    }

    [Fact]
    public async Task SearchAsync_ServiceFails_PropagatesException()
    {
        _service
            .Setup(s => s.SearchAsync(It.IsAny<string>(), It.IsAny<WebSearchTopic>(), It.IsAny<WebSearchTimeRange>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new HttpRequestException("quota"));
        var tool = new WebSearchTool(_service.Object, maxSearches: 2);

        await Assert.ThrowsAsync<HttpRequestException>(() => tool.SearchAsync("a", WebSearchTopic.General, WebSearchTimeRange.Any, default));
    }

    [Fact]
    public void AsAIFunction_Schema_NamesToolAndUsesEnumNames()
    {
        var function = new WebSearchTool(_service.Object, maxSearches: 2).AsAIFunction();

        Assert.Equal(WebSearchTool.Name, function.Name);
        var schema = function.JsonSchema.ToString();
        Assert.Contains("\"Finance\"", schema);
        Assert.Contains("\"Month\"", schema);
    }
}

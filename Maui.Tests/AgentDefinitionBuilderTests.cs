using AgentAI;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using OpenAI.Responses;
using Xunit;

namespace InvestissementsDashboard.Maui.Tests;

#pragma warning disable OPENAI001 // OpenAI.Responses types are for evaluation purposes only and subject to change.

public class AgentDefinitionBuilderTests
{
    private readonly Mock<INewsService> _newsService = new();
    private readonly Mock<IWebSearchService> _webSearchService = new();
    private readonly Mock<IWeatherService> _weatherService = new();

    [Fact]
    public async Task ChatBuildAsync_Always_ReturnsPlainAssistantWithoutTools()
    {
        var definition = await new ChatAgentDefinitionBuilder().BuildAsync(null, null);

        Assert.Null(definition.Tools);
        Assert.Null(definition.AIContextProviders);
        Assert.Equal("chat-history-chat.json", definition.HistoryFileName);
    }

    [Fact]
    public async Task WeatherBuildAsync_WithCity_ExposesWeatherToolAndContext()
    {
        var definition = await new WeatherAgentDefinitionBuilder(_weatherService.Object).BuildAsync("Paris", null);

        var tool = Assert.Single(definition.Tools!);
        Assert.Equal(nameof(IWeatherService.GetWeather), tool.Name);
        Assert.NotNull(definition.AIContextProviders);
        Assert.Equal("chat-history-weather.json", definition.HistoryFileName);
    }

    [Fact]
    public async Task StockBuildAsync_WithName_LoadsInstructionsAndContextWithoutTools()
    {
        var definition = await new StockAgentDefinitionBuilder(TimeProvider.System).BuildAsync("ASML", "NL0010273215");

        Assert.Equal(InstructionLoader.Load("AnalyseAction.md"), definition.Instructions);
        Assert.Null(definition.Tools);
        Assert.NotNull(definition.AIContextProviders);
        Assert.Equal("chat-history-stock.json", definition.HistoryFileName);
    }

    [Fact]
    public async Task NewsBuildAsync_WithCompany_ExposesNewsTool()
    {
        var definition = await new NewsAgentDefinitionBuilder(_newsService.Object, TimeProvider.System).BuildAsync("ASML", null);

        Assert.Equal(InstructionLoader.Load("ActualitesSociete.md"), definition.Instructions);
        Assert.Equal(NewsAgentDefinitionBuilder.ToolName, Assert.Single(definition.Tools!).Name);
        Assert.NotNull(definition.AIContextProviders);
        Assert.Equal("chat-history-news.json", definition.HistoryFileName);
    }

    [Fact]
    public async Task NewsBuildAsync_ToolInvoked_QueriesServiceOverFixedLookback()
    {
        _newsService
            .Setup(s => s.GetLatestNewsAsync(It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);
        var definition = await new NewsAgentDefinitionBuilder(_newsService.Object, TimeProvider.System).BuildAsync("ASML", null);
        var tool = (AIFunction)Assert.Single(definition.Tools!);

        await tool.InvokeAsync(new AIFunctionArguments { ["company"] = "ASML", ["isin"] = "NL0010273215" });

        _newsService.Verify(s => s.GetLatestNewsAsync("ASML", "NL0010273215", NewsAgentDefinitionBuilder.LookbackDays, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ContextBuildAsync_Always_ExposesSearchToolWithLowEffortAndNoHistory()
    {
        var definition = await new ContextAgentDefinitionBuilder(_webSearchService.Object).BuildAsync(null, null);

        Assert.Equal(InstructionLoader.Load("Context.md"), definition.Instructions);
        Assert.Equal(AgentAI.WebSearchTool.Name, Assert.Single(definition.Tools!).Name);
        Assert.Null(definition.AIContextProviders);
        Assert.Equal(ResponseReasoningEffortLevel.Low, definition.ReasoningEffort);
        Assert.False(definition.KeepHistory);
        Assert.Equal("chat-history-context.json", definition.HistoryFileName);
    }

    [Fact]
    public async Task ContextBuildAsync_TwoAgents_GetDistinctSearchTools()
    {
        // The search cap is counted per tool instance: sharing one would make a second run inherit the first's count.
        var builder = new ContextAgentDefinitionBuilder(_webSearchService.Object);

        var first = await builder.BuildAsync(null, null);
        var second = await builder.BuildAsync(null, null);

        Assert.NotSame(Assert.Single(first.Tools!), Assert.Single(second.Tools!));
    }

    [Fact]
    public async Task AllocationBuildAsync_NoMcpUrl_ThrowsInvalidOperation()
    {
        var connector = new InvestZaptoMcpClient(new HttpClient(), NullLoggerFactory.Instance);
        await using var tools = new InvestZaptoMcpToolSource(connector, endpoint: null);

        await Assert.ThrowsAsync<InvalidOperationException>(() => new AllocationAgentDefinitionBuilder(tools, TimeProvider.System).BuildAsync(null, null));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task BuildAsync_MissingRequiredInput_ThrowsArgumentException(string? input)
    {
        IAgentDefinitionBuilder[] builders =
        [
            new WeatherAgentDefinitionBuilder(_weatherService.Object),
            new StockAgentDefinitionBuilder(TimeProvider.System),
            new NewsAgentDefinitionBuilder(_newsService.Object, TimeProvider.System),
        ];

        foreach (var builder in builders)
        {
            var ex = await Assert.ThrowsAsync<ArgumentException>(() => builder.BuildAsync(input, null));
            Assert.StartsWith("Cet agent nécessite", ex.Message);
        }
    }
}

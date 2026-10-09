using AgentAI;
using InvestissementsDashboard.Client.Services;
using InvestissementsDashboard.Maui.Services;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Moq.Protected;
using Xunit;

namespace InvestissementsDashboard.Maui.Tests;

public class AgentRunnerTests
{
    private readonly Mock<IAgentSettings> _settings = new();
    private readonly Mock<IAgentFactory> _factory = new();
    private readonly Mock<ICustomChatHistoryProvider> _history = new();
    private readonly Mock<AIAgent> _agent = new();
    private AgentAIOptions? _receivedOptions;

    public AgentRunnerTests()
    {
        _settings.SetupGet(s => s.FoundryEndpoint).Returns("https://foundry.test/");
        _factory
            .Setup(f => f.CreateAsync(It.IsAny<AgentKind>(), It.IsAny<string?>(), It.IsAny<string?>()))
            .ReturnsAsync((_agent.Object, _history.Object));
        _agent
            .Protected()
            .Setup<ValueTask<AgentSession>>("CreateSessionCoreAsync", ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new Mock<AgentSession>().Object);
    }

    private AgentRunner CreateRunner() => new(
        new AgentOptionsProvider(_settings.Object, new Uri("https://invest.test/"), @"C:\history"),
        options =>
        {
            _receivedOptions = options;
            return _factory.Object;
        },
        NullLogger<AgentRunner>.Instance);

    private void SetupRun(Func<Task<AgentResponse>> run) =>
        _agent
            .Protected()
            .Setup<Task<AgentResponse>>(
                "RunCoreAsync",
                ItExpr.IsAny<IEnumerable<ChatMessage>>(),
                ItExpr.IsAny<AgentSession?>(),
                ItExpr.IsAny<AgentRunOptions?>(),
                ItExpr.IsAny<CancellationToken>())
            .Returns(run);

    [Theory]
    [InlineData(AgentChoice.Stock, AgentKind.Stock)]
    [InlineData(AgentChoice.News, AgentKind.News)]
    public async Task RunAsync_Choice_CreatesMatchingAgentWithAssetName(AgentChoice choice, AgentKind expected)
    {
        SetupRun(() => Task.FromResult(new AgentResponse(new ChatMessage(ChatRole.Assistant, "ok"))));

        var result = await CreateRunner().RunAsync(choice, "Air Liquide");

        Assert.Equal("ok", result);
        _factory.Verify(f => f.CreateAsync(expected, "Air Liquide", null), Times.Once);
    }

    [Fact]
    public async Task RunAsync_Called_UsesOptionsFromSettings()
    {
        _settings.SetupGet(s => s.Model).Returns("gpt-x");
        SetupRun(() => Task.FromResult(new AgentResponse(new ChatMessage(ChatRole.Assistant, "ok"))));

        await CreateRunner().RunAsync(AgentChoice.News, "Air Liquide");

        Assert.Equal("https://foundry.test/", _receivedOptions!.FoundryProjectEndpoint);
        Assert.Equal("gpt-x", _receivedOptions.Model);
    }

    [Fact]
    public async Task RunAsync_MissingEndpoint_ThrowsBeforeCreatingFactory()
    {
        _settings.SetupGet(s => s.FoundryEndpoint).Returns((string?)null);

        await Assert.ThrowsAsync<InvalidOperationException>(() => CreateRunner().RunAsync(AgentChoice.Stock, "Air Liquide"));

        Assert.Null(_receivedOptions);
    }

    [Fact]
    public async Task RunAsync_AgentFails_ResetsHistoryAndRethrows()
    {
        SetupRun(() => throw new InvalidOperationException("boom"));

        await Assert.ThrowsAsync<InvalidOperationException>(() => CreateRunner().RunAsync(AgentChoice.Stock, "Air Liquide"));

        _history.Verify(h => h.ResetWithBackup(It.IsAny<string>()), Times.Once);
    }

    [Fact]
    public async Task RunAsync_Cancelled_KeepsHistory()
    {
        SetupRun(() => throw new OperationCanceledException());

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => CreateRunner().RunAsync(AgentChoice.Stock, "Air Liquide"));

        _history.Verify(h => h.ResetWithBackup(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task RunAsync_Completed_DisposesFactory()
    {
        SetupRun(() => Task.FromResult(new AgentResponse(new ChatMessage(ChatRole.Assistant, "ok"))));

        await CreateRunner().RunAsync(AgentChoice.Stock, "Air Liquide");

        _factory.Verify(f => f.DisposeAsync(), Times.Once);
    }
}

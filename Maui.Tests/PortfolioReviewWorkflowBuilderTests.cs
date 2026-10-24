using AgentAI;
using InvestissementsDashboard.Shared.Models;
using Microsoft.Agents.AI;
using Microsoft.Agents.AI.Workflows;
using Microsoft.Extensions.AI;
using Moq;
using Moq.Protected;
using Xunit;

namespace InvestissementsDashboard.Maui.Tests;

public class PortfolioReviewWorkflowBuilderTests
{
    private const string AllocationJson = """
        {
          "date": "2026-10-19",
          "totalValue": 1500.0,
          "lines": [
            { "name": "MSCI World", "isin": "IE00B4L5Y983", "assetClass": "Stocks", "sector": "Diversified", "region": "World", "value": 1500.0, "weight": 100.0, "unrealizedGain": 200.0, "roi": 25.0 }
          ]
        }
        """;

    private const string ContextJson = """
        {
          "date": "2026-10-19",
          "situation": "BCE en pause.",
          "factors": [],
          "indicators": []
        }
        """;

    private readonly Mock<IAgentFactory> _factory = new();
    private readonly Mock<ICustomChatHistoryProvider> _history = new();
    private readonly Mock<AIAgent> _agent = new();
    private readonly Mock<AIAgent> _contextAgent = new();
    private readonly Mock<IAssetDistributionSource> _distributionSource = new();
    private readonly PortfolioReviewWorkflowBuilder _builder = new();

    public PortfolioReviewWorkflowBuilderTests()
    {
        _factory
            .Setup(f => f.CreateAsync(It.IsAny<AgentKind>(), It.IsAny<string?>(), It.IsAny<string?>()))
            .ReturnsAsync((_agent.Object, _history.Object));
        // Declared after the catch-all above, so it takes precedence for the Context agent.
        _factory
            .Setup(f => f.CreateAsync(AgentKind.Context, It.IsAny<string?>(), It.IsAny<string?>()))
            .ReturnsAsync((_contextAgent.Object, _history.Object));
        foreach (var agent in new[] { _agent, _contextAgent })
        {
            agent
                .Protected()
                .Setup<ValueTask<AgentSession>>("CreateSessionCoreAsync", ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(new Mock<AgentSession>().Object);
        }
        SetupAgent(() => Task.FromResult(new AgentResponse(new ChatMessage(ChatRole.Assistant, AllocationJson))));
        SetupAgent(() => Task.FromResult(new AgentResponse(new ChatMessage(ChatRole.Assistant, ContextJson))), _contextAgent);
        _distributionSource
            .Setup(s => s.GetGeographyAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([new DistributionDto("USA", 1_500m, 100m)]);
        _distributionSource
            .Setup(s => s.GetSectorsAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([new DistributionDto("Technology", 1_500m, 100m)]);
    }

    private void SetupAgent(Func<Task<AgentResponse>> run, Mock<AIAgent>? agent = null) =>
        (agent ?? _agent)
            .Protected()
            .Setup<Task<AgentResponse>>(
                "RunCoreAsync",
                ItExpr.IsAny<IEnumerable<ChatMessage>>(),
                ItExpr.IsAny<AgentSession?>(),
                ItExpr.IsAny<AgentRunOptions?>(),
                ItExpr.IsAny<CancellationToken>())
            .Returns(run);

    // Runs the built workflow to its end (or to its first failure) and returns every event it published.
    private async Task<List<WorkflowEvent>> RunAsync()
    {
        var workflow = await _builder.BuildAsync(_factory.Object, _distributionSource.Object);
        await using var run = await InProcessExecution.RunStreamingAsync(workflow, new AnalysisRequest(new DateOnly(2026, 10, 19)));

        var events = new List<WorkflowEvent>();
        await foreach (var evt in run.WatchStreamAsync())
        {
            events.Add(evt);
            if (evt is WorkflowOutputEvent or ExecutorFailedEvent or WorkflowErrorEvent)
            {
                break;
            }
        }

        return events;
    }

    [Fact]
    public async Task BuildAsync_Always_CreatesTheAllocationAgent()
    {
        await _builder.BuildAsync(_factory.Object, _distributionSource.Object);

        _factory.Verify(f => f.CreateAsync(AgentKind.Allocation, null, null), Times.Once);
    }

    [Fact]
    public async Task BuildAsync_Always_CreatesTheContextAgent()
    {
        await _builder.BuildAsync(_factory.Object, _distributionSource.Object);

        _factory.Verify(f => f.CreateAsync(AgentKind.Context, null, null), Times.Once);
    }

    [Fact]
    public async Task BuildAsync_Run_EmitsContextBeforeSynthesis()
    {
        var events = await RunAsync();

        int context = events.FindIndex(e => e is ContextRetrievedEvent);
        int synthesis = events.FindIndex(e => e is SynthesisWrittenEvent);
        Assert.True(context >= 0, "ContextRetrievedEvent missing");
        Assert.True(synthesis > context, "the synthesis must be written after the context");
    }

    [Fact]
    public async Task BuildAsync_ContextFails_ProducesNoSynthesis()
    {
        SetupAgent(() => throw new InvalidOperationException("web search unavailable"), _contextAgent);

        var events = await RunAsync();

        Assert.Contains(events, e => e is ExecutorFailedEvent failed && failed.ExecutorId == "context");
        Assert.DoesNotContain(events, e => e is SynthesisWrittenEvent or WorkflowOutputEvent);
    }

    [Fact]
    public async Task BuildAsync_RunWithAgentJson_OutputsEmptySynthesisReport()
    {
        var events = await RunAsync();

        var report = events.OfType<WorkflowOutputEvent>().Select(e => e.As<SynthesisReport>()).Single();
        Assert.NotNull(report);
        Assert.Empty(report.Warnings);
        Assert.Empty(report.Strengths);
        Assert.Empty(report.Weaknesses);
    }

    [Fact]
    public async Task BuildAsync_Run_EmitsAllocationThenSynthesisEvents()
    {
        var events = await RunAsync();

        int allocation = events.FindIndex(e => e is AllocationRetrievedEvent);
        int synthesis = events.FindIndex(e => e is SynthesisWrittenEvent);
        Assert.True(allocation >= 0, "AllocationRetrievedEvent missing");
        Assert.True(synthesis > allocation, "the synthesis must be written after the allocation");
    }

    [Fact]
    public async Task BuildAsync_AllocationFails_ProducesNoSynthesis()
    {
        SetupAgent(() => throw new InvalidOperationException("model unavailable"));

        var events = await RunAsync();

        Assert.Contains(events, e => e is ExecutorFailedEvent failed && failed.ExecutorId == "allocation");
        Assert.DoesNotContain(events, e => e is SynthesisWrittenEvent or WorkflowOutputEvent);
    }

    [Fact]
    public async Task BuildAsync_Run_EmitsDistributionBeforeSynthesis()
    {
        var events = await RunAsync();

        int distribution = events.FindIndex(e => e is AssetDistributionRetrievedEvent);
        int synthesis = events.FindIndex(e => e is SynthesisWrittenEvent);
        Assert.True(distribution >= 0, "AssetDistributionRetrievedEvent missing");
        Assert.True(synthesis > distribution, "the synthesis must be written after the distributions");
    }

    [Fact]
    public async Task BuildAsync_DistributionSourceFails_ProducesNoSynthesis()
    {
        _distributionSource
            .Setup(s => s.GetSectorsAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new HttpRequestException("Api unreachable"));

        var events = await RunAsync();

        Assert.Contains(events, e => e is ExecutorFailedEvent failed && failed.ExecutorId == "distribution");
        Assert.DoesNotContain(events, e => e is SynthesisWrittenEvent or WorkflowOutputEvent);
    }

    [Fact]
    public async Task BuildAsync_NullFactory_Throws()
    {
        await Assert.ThrowsAsync<ArgumentNullException>(() => _builder.BuildAsync(null!, _distributionSource.Object));
    }

    [Fact]
    public async Task BuildAsync_NullDistributionSource_Throws()
    {
        await Assert.ThrowsAsync<ArgumentNullException>(() => _builder.BuildAsync(_factory.Object, null!));
    }

    [Fact]
    public async Task BuildAsync_Cancelled_ThrowsOperationCanceled()
    {
        // CreateAsync takes no token: only the builder's WaitAsync can interrupt it.
        var never = new TaskCompletionSource<(AIAgent, ICustomChatHistoryProvider)>();
        _factory
            .Setup(f => f.CreateAsync(It.IsAny<AgentKind>(), It.IsAny<string?>(), It.IsAny<string?>()))
            .Returns(never.Task);
        using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(100));

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => _builder.BuildAsync(_factory.Object, _distributionSource.Object, cts.Token));
    }
}

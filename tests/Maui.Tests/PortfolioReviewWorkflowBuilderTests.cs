using AgentAI.Core.Agents;
using AgentAI.Core.Runtime;
using AgentAI.Core.Workflows.PortfolioReview;
using AgentAI.Core.Workflows.PortfolioReview.Events;
using AgentAI.Core.Workflows.PortfolioReview.Messages;
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
    private const string ContextJson = """
        {
          "date": "2026-10-19",
          "situation": "BCE en pause.",
          "factors": [
            { "description": "Pause de la BCE", "type": "Support", "assetClasses": ["Bonds"], "sectors": ["Government"], "regions": ["Europe"], "horizon": "ShortTerm", "sources": [] }
          ],
          "indicators": []
        }
        """;

    private const string SynthesisJson = """
        {
          "scenarios": [
            { "scenario": "S3 — Stagflation", "verdict": "Weakened", "facts": "Brent en repli", "portfolioImpact": "Moins de pression sur la duration" }
          ],
          "warnings": ["Concentration sur la technologie"],
          "strengths": ["Exposition mondiale"],
          "weaknesses": ["Peu d'obligations"],
          "recommendations": [
            { "level": "AssetClass", "target": "Bonds", "direction": "Reinforce", "driver": "BCE en pause", "rationale": "Les taux se stabilisent.", "confidence": "Medium" }
          ]
        }
        """;

    private readonly Mock<IAgentFactory> _factory = new();
    private readonly Mock<IFileChatHistoryProvider> _history = new();
    private readonly Mock<AIAgent> _contextAgent = new();
    private readonly Mock<AIAgent> _synthesisAgent = new();
    private readonly Mock<IAllocationSource> _allocationSource = new();
    private readonly Mock<IAssetDistributionSource> _distributionSource = new();
    private readonly PortfolioReviewWorkflowBuilder _builder = new();

    public PortfolioReviewWorkflowBuilderTests()
    {
        _factory
            .Setup(f => f.CreateAsync(AgentKind.Context, It.IsAny<string?>(), It.IsAny<string?>()))
            .ReturnsAsync((_contextAgent.Object, _history.Object));
        _factory
            .Setup(f => f.CreateAsync(AgentKind.Synthesis, It.IsAny<string?>(), It.IsAny<string?>()))
            .ReturnsAsync((_synthesisAgent.Object, _history.Object));
        foreach (var agent in new[] { _contextAgent, _synthesisAgent })
        {
            agent
                .Protected()
                .Setup<ValueTask<AgentSession>>("CreateSessionCoreAsync", ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(new Mock<AgentSession>().Object);
        }
        _allocationSource
            .Setup(s => s.GetAssetsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync([new AssetDto(1, "MSCI World", "Stocks", "PEA", "PEA TR", "ETF_Stocks", "Diversified", "", "World", 2, 100m, 0m, 0m, 1_500m, 50m, null, 5m, 100m)]);
        _allocationSource
            .Setup(s => s.GetLastSnapshotAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync((SnapshotDto?)null);
        _allocationSource
            .Setup(s => s.GetWatchlistAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);
        SetupAgent(() => Task.FromResult(new AgentResponse(new ChatMessage(ChatRole.Assistant, ContextJson))), _contextAgent);
        SetupAgent(() => Task.FromResult(new AgentResponse(new ChatMessage(ChatRole.Assistant, SynthesisJson))), _synthesisAgent);
        _distributionSource
            .Setup(s => s.GetGeographyAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([new DistributionDto("USA", 1_500m, 100m)]);
        _distributionSource
            .Setup(s => s.GetSectorsAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([new DistributionDto("Technology", 1_500m, 100m)]);
    }

    private void SetupAgent(Func<Task<AgentResponse>> run, Mock<AIAgent> agent) =>
        agent
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
        var workflow = await _builder.BuildAsync(_factory.Object, _allocationSource.Object, _distributionSource.Object);
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

    // The runner follows the steps through these ids: a step renamed or never announced would stay "pending" on screen.
    [Fact]
    public async Task BuildAsync_Run_AnnouncesEveryStepUnderItsPublicId()
    {
        var events = await RunAsync();

        var invoked = events.OfType<ExecutorInvokedEvent>().Select(e => e.ExecutorId).ToList();
        Assert.Contains(PortfolioReviewSteps.Allocation, invoked);
        Assert.Contains(PortfolioReviewSteps.Context, invoked);
        Assert.Contains(PortfolioReviewSteps.Distribution, invoked);
        Assert.Contains(PortfolioReviewSteps.Synthesis, invoked);
    }

    [Fact]
    public async Task BuildAsync_Always_CreatesTheContextAgent()
    {
        await _builder.BuildAsync(_factory.Object, _allocationSource.Object, _distributionSource.Object);

        _factory.Verify(f => f.CreateAsync(AgentKind.Context, null, null), Times.Once);
    }

    [Fact]
    public async Task BuildAsync_Always_CreatesTheSynthesisAgent()
    {
        await _builder.BuildAsync(_factory.Object, _allocationSource.Object, _distributionSource.Object);

        _factory.Verify(f => f.CreateAsync(AgentKind.Synthesis, null, null), Times.Once);
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
    public async Task BuildAsync_ContextWithoutFactors_ProducesNoSynthesis()
    {
        const string emptyContext = """{ "date": "2026-10-19", "situation": "", "factors": [], "indicators": [] }""";
        SetupAgent(() => Task.FromResult(new AgentResponse(new ChatMessage(ChatRole.Assistant, emptyContext))), _contextAgent);

        var events = await RunAsync();

        Assert.Contains(events, e => e is ExecutorFailedEvent failed && failed.ExecutorId == "context");
        Assert.DoesNotContain(events, e => e is SynthesisWrittenEvent or WorkflowOutputEvent);
    }

    [Fact]
    public async Task BuildAsync_RunWithAgentJson_OutputsTheSynthesisAgentReport()
    {
        var events = await RunAsync();

        var report = events.OfType<WorkflowOutputEvent>().Select(e => e.As<SynthesisReport>()).Single();
        Assert.NotNull(report);
        Assert.Equal(new ScenarioAssessment("S3 — Stagflation", ScenarioVerdict.Weakened, "Brent en repli", "Moins de pression sur la duration"), Assert.Single(report.Scenarios));
        Assert.Equal(["Concentration sur la technologie"], report.Warnings);
        Assert.Equal(new Recommendation(RecommendationLevel.AssetClass, "Bonds", RecommendationDirection.Reinforce, "BCE en pause", "Les taux se stabilisent.", RecommendationConfidence.Medium), Assert.Single(report.Recommendations));
    }

    [Fact]
    public async Task BuildAsync_SynthesisAgentFails_ProducesNoOutput()
    {
        SetupAgent(() => throw new InvalidOperationException("model unavailable"), _synthesisAgent);

        var events = await RunAsync();

        Assert.Contains(events, e => e is ExecutorFailedEvent failed && failed.ExecutorId == "synthesis");
        Assert.DoesNotContain(events, e => e is SynthesisWrittenEvent or WorkflowOutputEvent);
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
        _allocationSource
            .Setup(s => s.GetAssetsAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new HttpRequestException("Api unreachable"));

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
        await Assert.ThrowsAsync<ArgumentNullException>(() => _builder.BuildAsync(null!, _allocationSource.Object, _distributionSource.Object));
    }

    [Fact]
    public async Task BuildAsync_NullAllocationSource_Throws()
    {
        await Assert.ThrowsAsync<ArgumentNullException>(() => _builder.BuildAsync(_factory.Object, null!, _distributionSource.Object));
    }

    [Fact]
    public async Task BuildAsync_Always_NeverCreatesAnAgentForTheAllocation()
    {
        await RunAsync();

        _factory.Verify(f => f.CreateAsync(It.IsAny<AgentKind>(), It.IsAny<string?>(), It.IsAny<string?>()), Times.Exactly(2));
    }

    [Fact]
    public async Task BuildAsync_NullDistributionSource_Throws()
    {
        await Assert.ThrowsAsync<ArgumentNullException>(() => _builder.BuildAsync(_factory.Object, _allocationSource.Object, null!));
    }

    [Fact]
    public async Task BuildAsync_Cancelled_ThrowsOperationCanceled()
    {
        // CreateAsync takes no token: only the builder's WaitAsync can interrupt it.
        var never = new TaskCompletionSource<(AIAgent, IFileChatHistoryProvider)>();
        _factory
            .Setup(f => f.CreateAsync(It.IsAny<AgentKind>(), It.IsAny<string?>(), It.IsAny<string?>()))
            .Returns(never.Task);
        using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(100));

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => _builder.BuildAsync(_factory.Object, _allocationSource.Object, _distributionSource.Object, cts.Token));
    }
}

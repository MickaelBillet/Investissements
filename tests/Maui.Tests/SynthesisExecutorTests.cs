using AgentAI.Core.Workflows.PortfolioReview.Events;
using AgentAI.Core.Workflows.PortfolioReview.Executors;
using AgentAI.Core.Workflows.PortfolioReview.Messages;
using InvestissementsDashboard.Shared.Models;
using Microsoft.Agents.AI;
using Microsoft.Agents.AI.Workflows;
using Microsoft.Extensions.AI;
using Moq;
using Moq.Protected;
using Xunit;

namespace InvestissementsDashboard.Maui.Tests;

/// <summary>
/// Exercises the synthesis behind a real fan-in barrier, fed by test sources standing for the allocation, the
/// context and the asset distributions, so the "waits for every input" behaviour is tested independently of the agents.
/// </summary>
public class SynthesisExecutorTests
{
    private static readonly DateOnly Date = new(2026, 10, 19);

    private const string ReportJson = """
        {
          "warnings": ["Concentration"],
          "strengths": [],
          "weaknesses": [],
          "recommendations": [
            { "level": "Theme", "target": "Défense", "direction": "Watch", "driver": "Hausse des budgets", "rationale": "À suivre.", "confidence": "Low" }
          ]
        }
        """;

    private readonly Mock<AIAgent> _agent = new();
    private readonly List<string> _prompts = [];

    public SynthesisExecutorTests()
    {
        _agent
            .Protected()
            .Setup<ValueTask<AgentSession>>("CreateSessionCoreAsync", ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new Mock<AgentSession>().Object);
        _agent
            .Protected()
            .Setup<Task<AgentResponse>>(
                "RunCoreAsync",
                ItExpr.IsAny<IEnumerable<ChatMessage>>(),
                ItExpr.IsAny<AgentSession?>(),
                ItExpr.IsAny<AgentRunOptions?>(),
                ItExpr.IsAny<CancellationToken>())
            .Callback<IEnumerable<ChatMessage>, AgentSession?, AgentRunOptions?, CancellationToken>((messages, _, _, _) => _prompts.Add(string.Join("\n", messages.Select(m => m.Text))))
            .ReturnsAsync(new AgentResponse(new ChatMessage(ChatRole.Assistant, ReportJson)));
    }

    private async Task<List<WorkflowEvent>> RunAsync(TimeSpan allocationDelay, TimeSpan? distributionDelay = null)
    {
        var start = new StartExecutor();
        var allocation = new DelayedSource<AllocationResult>("test-allocation", allocationDelay, new AllocationResult(Date.AddDays(-1), 1500m, [], [new("ASML", "NL0010273215")]));
        var context = new DelayedSource<PortfolioContext>("test-context", TimeSpan.Zero, new PortfolioContext(Date, "BCE en pause, côté taux", [], []));
        var distribution = new DelayedSource<DistributionResult>("test-distribution", distributionDelay ?? TimeSpan.Zero, new DistributionResult([new AssetClassDistribution("Stocks", [], [])]));
        var synthesis = new SynthesisExecutor(_agent.Object);

        var workflow = new WorkflowBuilder(start)
            .AddFanOutEdge(start, [allocation, context, distribution])
            .AddFanInBarrierEdge([allocation, context, distribution], synthesis)
            .WithOutputFrom(synthesis)
            .Build();

        await using var run = await InProcessExecution.RunStreamingAsync(workflow, new AnalysisRequest(Date));
        var events = new List<WorkflowEvent>();
        await foreach (var evt in run.WatchStreamAsync())
        {
            events.Add(evt);
        }

        return events;
    }

    [Fact]
    public async Task Synthesis_AllInputsArrive_YieldsOneReport()
    {
        var events = await RunAsync(TimeSpan.Zero);

        Assert.Single(events.OfType<WorkflowOutputEvent>(), e => e.As<SynthesisReport>() is not null);
        Assert.Single(events.OfType<SynthesisWrittenEvent>());
    }

    [Fact]
    public async Task Synthesis_AllInputsArrive_CallsTheAgentOnce()
    {
        await RunAsync(TimeSpan.Zero);

        Assert.Single(_prompts);
    }

    [Fact]
    public async Task Synthesis_AllInputsArrive_SendsDateAndInputsToTheAgent()
    {
        await RunAsync(TimeSpan.Zero);

        var prompt = Assert.Single(_prompts);
        // The allocation's date is the model's and differs on purpose: the context carries the requested one.
        Assert.StartsWith("Rédige la synthèse du portefeuille au 2026-10-19.", prompt);
        Assert.Contains("1500", prompt);
        Assert.Contains("BCE en pause, côté taux", prompt);
        Assert.Contains("Stocks", prompt);
    }

    [Fact]
    public async Task Synthesis_AllInputsArrive_AnnouncesNoMissingInput()
    {
        await RunAsync(TimeSpan.Zero);

        var prompt = Assert.Single(_prompts);
        Assert.DoesNotContain("Non disponibles", prompt);
    }

    [Fact]
    public async Task Synthesis_AllInputsArrive_SendsWatchlist()
    {
        await RunAsync(TimeSpan.Zero);

        var prompt = Assert.Single(_prompts);
        Assert.Contains("NL0010273215", prompt);
    }

    [Fact]
    public async Task Synthesis_AgentAnswers_YieldsItsReport()
    {
        var events = await RunAsync(TimeSpan.Zero);

        var report = events.OfType<WorkflowOutputEvent>().Select(e => e.As<SynthesisReport>()).Single();
        Assert.NotNull(report);
        Assert.Equal(["Concentration"], report.Warnings);
        Assert.Equal(new Recommendation(RecommendationLevel.Theme, "Défense", RecommendationDirection.Watch, "Hausse des budgets", "À suivre.", RecommendationConfidence.Low), Assert.Single(report.Recommendations));
    }

    [Fact]
    public void SynthesisWrittenEvent_ToString_CountsWarningsAndRecommendations()
    {
        var report = new SynthesisReport(["a", "b"], [], [], [new Recommendation(RecommendationLevel.Line, "x", RecommendationDirection.Reduce, "d", "r", RecommendationConfidence.High)]);

        Assert.Equal("Synthèse : 2 avertissement(s), 1 recommandation(s)", new SynthesisWrittenEvent(report).ToString());
    }

    [Fact]
    public async Task Synthesis_AllocationSlowerThanContext_StillWaitsForBoth()
    {
        var events = await RunAsync(TimeSpan.FromMilliseconds(200));

        int allocationDone = events.FindIndex(e => e is ExecutorCompletedEvent completed && completed.ExecutorId == "test-allocation");
        int output = events.FindIndex(e => e is WorkflowOutputEvent);
        Assert.True(allocationDone >= 0, "the slow source never completed");
        Assert.True(output > allocationDone, "the synthesis must not be written before the slow input arrived");
        Assert.Single(events.OfType<WorkflowOutputEvent>());
    }

    [Fact]
    public async Task Synthesis_DistributionSlowerThanOthers_StillWaitsForIt()
    {
        var events = await RunAsync(TimeSpan.Zero, TimeSpan.FromMilliseconds(200));

        int distributionDone = events.FindIndex(e => e is ExecutorCompletedEvent completed && completed.ExecutorId == "test-distribution");
        int output = events.FindIndex(e => e is WorkflowOutputEvent);
        Assert.True(distributionDone >= 0, "the slow source never completed");
        Assert.True(output > distributionDone, "the synthesis must not be written before the distributions arrived");
        Assert.Single(events.OfType<WorkflowOutputEvent>());
    }

    private sealed class DelayedSource<T>(string id, TimeSpan delay, T result) : Executor<AnalysisRequest, T>(id)
    {
        public override async ValueTask<T> HandleAsync(AnalysisRequest message, IWorkflowContext context, CancellationToken cancellationToken = default)
        {
            await Task.Delay(delay, cancellationToken);
            return result;
        }
    }
}

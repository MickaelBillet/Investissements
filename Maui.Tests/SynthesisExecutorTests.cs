using AgentAI;
using InvestissementsDashboard.Shared.Models;
using Microsoft.Agents.AI.Workflows;
using Xunit;

namespace InvestissementsDashboard.Maui.Tests;

/// <summary>
/// Exercises the synthesis behind a real fan-in barrier, fed by test sources standing for the allocation, the
/// context and the asset distributions, so the "waits for every input" behaviour is tested independently of the agents.
/// </summary>
public class SynthesisExecutorTests
{
    private static readonly DateOnly Date = new(2026, 10, 19);

    private static async Task<List<WorkflowEvent>> RunAsync(TimeSpan allocationDelay, TimeSpan? distributionDelay = null)
    {
        var start = new StartExecutor();
        var allocation = new DelayedSource<AllocationResult>("test-allocation", allocationDelay, new AllocationResult(Date, 0m, []));
        var context = new DelayedSource<PortfolioContext>("test-context", TimeSpan.Zero, new PortfolioContext(Date));
        var distribution = new DelayedSource<DistributionResult>("test-distribution", distributionDelay ?? TimeSpan.Zero, new DistributionResult([]));
        var synthesis = new SynthesisExecutor();

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

using Microsoft.Agents.AI.Workflows;

namespace AgentAI;

/// <summary>
/// Combines the allocation, the context and the asset distributions into the review's output. Skeleton: the
/// report is empty for now.
/// </summary>
/// <remarks>
/// The fan-in barrier delivers its three inputs as three separate messages, in no guaranteed order. Each handler
/// keeps its input in the workflow state (rather than in fields, so a checkpoint would capture it) and the report
/// is written once all three are there.
/// </remarks>
public sealed class SynthesisExecutor() : Executor("synthesis")
{
    private const string AllocationKey = "allocation";
    private const string ContextKey = "context";
    private const string DistributionKey = "distribution";

    protected override ProtocolBuilder ConfigureProtocol(ProtocolBuilder protocolBuilder) =>
        protocolBuilder
            .ConfigureRoutes(routes => routes
                .AddHandler<AllocationResult>(this.OnAllocationAsync)
                .AddHandler<PortfolioContext>(this.OnContextAsync)
                .AddHandler<DistributionResult>(this.OnDistributionAsync))
            .YieldsOutput<SynthesisReport>();

    private async ValueTask OnAllocationAsync(AllocationResult allocation, IWorkflowContext context, CancellationToken cancellationToken)
    {
        await context.QueueStateUpdateAsync(AllocationKey, allocation, cancellationToken);
        await this.WriteWhenCompleteAsync(context, cancellationToken);
    }

    private async ValueTask OnContextAsync(PortfolioContext portfolioContext, IWorkflowContext context, CancellationToken cancellationToken)
    {
        await context.QueueStateUpdateAsync(ContextKey, portfolioContext, cancellationToken);
        await this.WriteWhenCompleteAsync(context, cancellationToken);
    }

    private async ValueTask OnDistributionAsync(DistributionResult distribution, IWorkflowContext context, CancellationToken cancellationToken)
    {
        await context.QueueStateUpdateAsync(DistributionKey, distribution, cancellationToken);
        await this.WriteWhenCompleteAsync(context, cancellationToken);
    }

    private async ValueTask WriteWhenCompleteAsync(IWorkflowContext context, CancellationToken cancellationToken)
    {
        var allocation = await context.ReadStateAsync<AllocationResult>(AllocationKey, cancellationToken);
        var portfolioContext = await context.ReadStateAsync<PortfolioContext>(ContextKey, cancellationToken);
        var distribution = await context.ReadStateAsync<DistributionResult>(DistributionKey, cancellationToken);
        if (allocation is null || portfolioContext is null || distribution is null)
        {
            return;
        }

        var report = new SynthesisReport([], [], []);

        await context.AddEventAsync(new SynthesisWrittenEvent(report), cancellationToken);
        await context.YieldOutputAsync(report, cancellationToken);
    }
}

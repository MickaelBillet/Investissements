using AgentAI.Core.Agents;
using AgentAI.Core.Workflows.PortfolioReview.Executors;
using Microsoft.Agents.AI.Workflows;

namespace AgentAI.Core.Workflows.PortfolioReview;

public sealed class PortfolioReviewWorkflowBuilder : IPortfolioReviewWorkflowBuilder
{
    /// <summary>
    /// Builds the workflow: the allocation, the context and the asset distributions run in parallel, then the
    /// synthesis combines them once all three are done; the synthesis report is the workflow's output.
    /// </summary>
    /// <remarks>
    /// The fan-in barrier holds the results until each parallel step has produced one, then delivers them all to
    /// the synthesis in the following step.
    /// </remarks>
    public async Task<Workflow> BuildAsync(IAgentFactory agentFactory, IAllocationSource allocationSource, IAssetDistributionSource distributionSource, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(agentFactory);
        ArgumentNullException.ThrowIfNull(allocationSource);
        ArgumentNullException.ThrowIfNull(distributionSource);

        var start = new StartExecutor();
        var allocation = this.CreateAllocationExecutor(allocationSource);
        var context = await this.CreateContextExecutorAsync(agentFactory, cancellationToken).ConfigureAwait(false);
        var distribution = this.CreateAssetDistributionExecutor(distributionSource);
        var synthesis = await this.CreateSynthesisExecutorAsync(agentFactory, cancellationToken).ConfigureAwait(false);

        return new WorkflowBuilder(start)
            .AddFanOutEdge(start, [allocation, context, distribution])
            .AddFanInBarrierEdge([allocation, context, distribution], synthesis)
            .WithOutputFrom(synthesis)
            .Build();
    }

    private async Task<ContextExecutor> CreateContextExecutorAsync(IAgentFactory agentFactory, CancellationToken cancellationToken)
    {
        var (agent, _) = await agentFactory.CreateAsync(AgentKind.Context).WaitAsync(cancellationToken).ConfigureAwait(false);
        return new ContextExecutor(agent);
    }

    private async Task<SynthesisExecutor> CreateSynthesisExecutorAsync(IAgentFactory agentFactory, CancellationToken cancellationToken)
    {
        var (agent, _) = await agentFactory.CreateAsync(AgentKind.Synthesis).WaitAsync(cancellationToken).ConfigureAwait(false);
        return new SynthesisExecutor(agent);
    }

    private AssetDistributionExecutor CreateAssetDistributionExecutor(IAssetDistributionSource distributionSource)
    {
        return new AssetDistributionExecutor(distributionSource);
    }

    private AllocationExecutor CreateAllocationExecutor(IAllocationSource allocationSource)
    {
        return new AllocationExecutor(allocationSource);
    }
}

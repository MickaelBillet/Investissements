using Microsoft.Agents.AI.Workflows;

namespace AgentAI;

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
    public async Task<Workflow> BuildAsync(IAgentFactory agentFactory, IAssetDistributionSource distributionSource, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(agentFactory);
        ArgumentNullException.ThrowIfNull(distributionSource);

        var start = new StartExecutor();
        var allocation = await CreateAllocationExecutorAsync(agentFactory, cancellationToken).ConfigureAwait(false);
        var context = await CreateContextExecutorAsync(agentFactory, cancellationToken).ConfigureAwait(false);
        var distribution = new AssetDistributionExecutor(distributionSource);
        var synthesis = new SynthesisExecutor();

        return new WorkflowBuilder(start)
            .AddFanOutEdge(start, [allocation, context, distribution])
            .AddFanInBarrierEdge([allocation, context, distribution], synthesis)
            .WithOutputFrom(synthesis)
            .Build();
    }

    // Built on the existing Allocation agent, whose instructions already produce an AllocationResult JSON.
    private static async Task<AllocationExecutor> CreateAllocationExecutorAsync(IAgentFactory agentFactory, CancellationToken cancellationToken)
    {
        // The history provider is not needed here: the Allocation agent does not replay its history.
        var (agent, _) = await agentFactory.CreateAsync(AgentKind.Allocation).WaitAsync(cancellationToken).ConfigureAwait(false);
        return new AllocationExecutor(agent);
    }

    private static async Task<ContextExecutor> CreateContextExecutorAsync(IAgentFactory agentFactory, CancellationToken cancellationToken)
    {
        var (agent, _) = await agentFactory.CreateAsync(AgentKind.Context).WaitAsync(cancellationToken).ConfigureAwait(false);
        return new ContextExecutor(agent);
    }
}

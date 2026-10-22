using Microsoft.Agents.AI.Workflows;

namespace AgentAI;

/// <summary>
/// Gathers the context the synthesis needs besides the allocation. Skeleton: it runs in parallel with
/// <see cref="AllocationExecutor"/> but produces no content yet.
/// </summary>
public sealed class ContextExecutor() : Executor<AnalysisRequest, PortfolioContext>("context")
{
    public override ValueTask<PortfolioContext> HandleAsync(AnalysisRequest message, IWorkflowContext context, CancellationToken cancellationToken = default) =>
        ValueTask.FromResult(new PortfolioContext(message.Date));
}

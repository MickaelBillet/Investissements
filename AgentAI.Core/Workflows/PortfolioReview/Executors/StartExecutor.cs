using AgentAI.Core.Workflows.PortfolioReview.Messages;
using Microsoft.Agents.AI.Workflows;

namespace AgentAI.Core.Workflows.PortfolioReview.Executors;

/// <summary>
/// Entry point of the workflow: a workflow has a single start executor, and the fan-out towards the parallel
/// steps has to leave from an executor, so this one only forwards the request.
/// </summary>
internal sealed class StartExecutor() : Executor<AnalysisRequest, AnalysisRequest>("start")
{
    public override ValueTask<AnalysisRequest> HandleAsync(AnalysisRequest message, IWorkflowContext context, CancellationToken cancellationToken = default) =>
        ValueTask.FromResult(message);
}

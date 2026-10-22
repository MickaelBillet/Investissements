using Microsoft.Agents.AI.Workflows;

namespace AgentAI;

/// <summary>
/// Entry point of the workflow: a workflow has a single start executor, and the fan-out towards the parallel
/// steps has to leave from an executor, so this one only forwards the request.
/// </summary>
public sealed class StartExecutor() : Executor<AnalysisRequest, AnalysisRequest>("start")
{
    public override ValueTask<AnalysisRequest> HandleAsync(AnalysisRequest message, IWorkflowContext context, CancellationToken cancellationToken = default) =>
        ValueTask.FromResult(message);
}

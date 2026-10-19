using InvestissementsDashboard.Shared.Models;
using Microsoft.Agents.AI;
using Microsoft.Agents.AI.Workflows;

namespace AgentAI;

public sealed class AllocationExecutor(AIAgent agent) : Executor<AnalysisRequest, AllocationResult>("allocation")
{
    public override async ValueTask<AllocationResult> HandleAsync(AnalysisRequest message, IWorkflowContext context, CancellationToken cancellationToken = default)
    {
        // French because it is the agent's working language; the field mapping lives in its instructions
        // (AllocationPortefeuille.md) so that the menu entry and the workflow share a single definition.
        var prompt = $"Dresse l'état de mon portefeuille au {message.Date:yyyy-MM-dd}.";

        // RunAsync<T> : schéma JSON envoyé au modèle + désérialisation en un seul appel.
        var response = await agent.RunAsync<AllocationResult>(prompt, cancellationToken: cancellationToken);
        var allocation = response.Result;

        await context.AddEventAsync(new AllocationRetrievedEvent(allocation), cancellationToken);
        return allocation;
    }
}

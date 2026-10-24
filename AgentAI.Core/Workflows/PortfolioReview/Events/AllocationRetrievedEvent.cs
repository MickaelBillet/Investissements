using Microsoft.Agents.AI.Workflows;

namespace AgentAI;

public sealed class AllocationRetrievedEvent(AllocationResult result) : WorkflowEvent(result)
{
    public override string ToString() => $"Allocation récupérée : {result.Lines.Count} lignes, {result.TotalValue:N0} €";
}

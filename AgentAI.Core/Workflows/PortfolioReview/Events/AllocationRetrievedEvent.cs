using AgentAI.Core.Workflows.PortfolioReview.Messages;
using Microsoft.Agents.AI.Workflows;

namespace AgentAI.Core.Workflows.PortfolioReview.Events;

public sealed class AllocationRetrievedEvent(AllocationResult result) : WorkflowEvent(result)
{
    public override string ToString() => $"Allocation récupérée : {result.Lines.Count} lignes, {result.TotalValue:N0} €";
}

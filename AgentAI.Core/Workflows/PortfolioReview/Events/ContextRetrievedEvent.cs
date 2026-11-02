using AgentAI.Core.Workflows.PortfolioReview.Messages;
using Microsoft.Agents.AI.Workflows;

namespace AgentAI.Core.Workflows.PortfolioReview.Events;

public sealed class ContextRetrievedEvent(PortfolioContext result) : WorkflowEvent(result)
{
    public override string ToString() =>
        $"Contexte récupéré : {result.Factors.Count} facteurs, {result.Indicators.Count(i => i.Value is not null)}/{result.Indicators.Count} indicateurs renseignés";
}

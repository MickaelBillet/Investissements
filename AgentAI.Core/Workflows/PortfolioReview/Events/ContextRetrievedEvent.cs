using Microsoft.Agents.AI.Workflows;

namespace AgentAI;

public sealed class ContextRetrievedEvent(PortfolioContext result) : WorkflowEvent(result)
{
    public override string ToString() =>
        $"Contexte récupéré : {result.Factors.Count} facteurs, {result.Indicators.Count(i => i.Value is not null)}/{result.Indicators.Count} indicateurs renseignés";
}

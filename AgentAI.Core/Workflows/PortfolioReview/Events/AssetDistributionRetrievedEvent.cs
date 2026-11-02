using AgentAI.Core.Workflows.PortfolioReview.Messages;
using Microsoft.Agents.AI.Workflows;

namespace AgentAI.Core.Workflows.PortfolioReview.Events;

public sealed class AssetDistributionRetrievedEvent(DistributionResult result) : WorkflowEvent(result)
{
    public override string ToString() =>
        "Répartitions récupérées : " + string.Join(", ", result.Classes.Select(c => $"{c.AssetClass} ({c.Geography.Count} zones, {c.Sectors.Count} secteurs)"));
}

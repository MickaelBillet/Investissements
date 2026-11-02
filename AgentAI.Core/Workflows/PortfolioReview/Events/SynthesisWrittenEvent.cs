using AgentAI.Core.Workflows.PortfolioReview.Messages;
using Microsoft.Agents.AI.Workflows;

namespace AgentAI.Core.Workflows.PortfolioReview.Events;

public sealed class SynthesisWrittenEvent(SynthesisReport report) : WorkflowEvent(report)
{
    public override string ToString() => $"Synthèse : {report.Warnings.Count} avertissement(s), {report.Recommendations.Count} recommandation(s)";
}

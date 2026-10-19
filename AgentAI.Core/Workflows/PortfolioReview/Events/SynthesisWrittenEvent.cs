using Microsoft.Agents.AI.Workflows;

namespace AgentAI;

public sealed class SynthesisWrittenEvent(SynthesisReport report) : WorkflowEvent(report)
{
    public override string ToString() => $"Synthèse : {report.Warnings.Count} avertissement(s)";
}

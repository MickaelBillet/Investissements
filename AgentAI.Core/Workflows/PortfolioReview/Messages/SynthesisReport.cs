namespace AgentAI;

public record SynthesisReport(
    IReadOnlyList<string> Warnings,
    IReadOnlyList<string> Strengths,
    IReadOnlyList<string> Weaknesses);
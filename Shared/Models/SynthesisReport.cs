namespace InvestissementsDashboard.Shared.Models;

public record SynthesisReport(
    IReadOnlyList<string> Warnings,
    IReadOnlyList<string> Strengths,
    IReadOnlyList<string> Weaknesses,
    IReadOnlyList<Recommendation> Recommendations);
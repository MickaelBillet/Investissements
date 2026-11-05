namespace InvestissementsDashboard.Client.Model;

/// <summary>What the page shows of a step, whereas <see cref="AnalysisStepUpdate"/> is what the runner reports.</summary>
/// <param name="Step">The step.</param>
/// <param name="Status">Its current status.</param>
/// <param name="Duration">Known once the step has ended.</param>
public record AnalysisStepState(AnalysisStep Step, AnalysisStepStatus Status, TimeSpan? Duration = null)
{
    public AnalysisPhase Phase => Step == AnalysisStep.Synthesis ? AnalysisPhase.Writing : AnalysisPhase.Collection;
}

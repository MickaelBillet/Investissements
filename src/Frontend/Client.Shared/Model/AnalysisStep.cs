namespace InvestissementsDashboard.Client.Model;

// Own enum rather than the workflow's ids: this library does not reference AgentAI.Core. Declared in display order.
public enum AnalysisStep
{
    Allocation,
    Distribution,
    Context,
    Synthesis
}

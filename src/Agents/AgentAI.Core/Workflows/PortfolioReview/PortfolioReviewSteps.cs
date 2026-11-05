namespace AgentAI.Core.Workflows.PortfolioReview;

/// <summary>
/// The executor ids of the workflow's steps, public so that a host can follow the steps through the framework's
/// events, which only carry these ids.
/// </summary>
public static class PortfolioReviewSteps
{
    public const string Allocation = "allocation";
    public const string Context = "context";
    public const string Distribution = "distribution";
    public const string Synthesis = "synthesis";
}

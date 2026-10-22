namespace InvestissementsDashboard.Client.Services;

/// <summary>
/// Runs the portfolio review workflow (allocation and context in parallel, then synthesis). Deliberately not registered by
/// <c>AddInvestissementsClient</c>: only the MAUI host can run agents, so the WASM site shows no analysis entry point.
/// </summary>
/// <remarks>
/// Kept apart from <see cref="IAgentRunner"/>: that seam returns one agent's free text, whereas this one runs a
/// multi-step workflow with a typed output.
/// </remarks>
public interface IPortfolioReviewRunner
{
    /// <summary>Completes once the workflow has produced its synthesis.</summary>
    /// <remarks>
    /// Returns no result while the synthesis is a skeleton; its output type will then be defined in
    /// <c>Shared/Models</c>, since this library does not reference the agents' one.
    /// </remarks>
    /// <param name="date">The date the review is asked for; forwarded to the workflow's first step.</param>
    Task RunAsync(DateOnly date, CancellationToken cancellationToken = default);
}

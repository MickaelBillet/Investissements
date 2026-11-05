using InvestissementsDashboard.Client.Model;
using InvestissementsDashboard.Shared.Models;

namespace InvestissementsDashboard.Client.Services;

/// <summary>
/// Runs the portfolio review workflow (allocation, context and distributions in parallel, then synthesis). Deliberately not registered by
/// <c>AddInvestissementsClient</c>: only the MAUI host can run agents, so the WASM site shows no analysis entry point.
/// </summary>
/// <remarks>
/// Kept apart from <see cref="IAgentRunner"/>: that seam returns one agent's free text, whereas this one runs a
/// multi-step workflow with a typed output.
/// </remarks>
public interface IPortfolioReviewRunner
{
    /// <summary>Completes once the workflow has produced its synthesis, and returns that report.</summary>
    /// <param name="date">The date the review is asked for; forwarded to the workflow's first step.</param>
    /// <param name="progress">Told when a step starts, ends or fails; may be called from a background thread.</param>
    /// <param name="cancellationToken">Interrupts the run.</param>
    Task<SynthesisReport> RunAsync(DateOnly date, IProgress<AnalysisStepUpdate>? progress = null, CancellationToken cancellationToken = default);
}

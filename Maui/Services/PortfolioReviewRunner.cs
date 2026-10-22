using System.Net;
using AgentAI;
using InvestissementsDashboard.Client.Services;
using Microsoft.Agents.AI.Workflows;
using Microsoft.Extensions.Logging;

namespace InvestissementsDashboard.Maui.Services;

/// <summary>
/// Runs the portfolio review workflow (built by <see cref="IPortfolioReviewWorkflowBuilder"/>) in process, until its synthesis step yields the report.
/// </summary>
/// <remarks>
/// Like <see cref="AgentRunner"/>, a factory is built for every run so that a change made in the settings page
/// is picked up without restarting the application; the distribution source is created per run as well, so this
/// singleton never holds the Api client it wraps.
/// </remarks>
public sealed class PortfolioReviewRunner(
    AgentOptionsProvider optionsProvider,
    Func<AgentAIOptions, IAgentFactory> factoryCreator,
    Func<IAssetDistributionSource> distributionSourceCreator,
    IPortfolioReviewWorkflowBuilder workflowBuilder,
    ILogger<PortfolioReviewRunner> logger,
    TimeSpan? timeout = null) : IPortfolioReviewRunner
{
    // Same bound as AgentRunner: the allocation step alone takes about a minute (several MCP tool calls).
    internal static readonly TimeSpan DefaultTimeout = TimeSpan.FromMinutes(3);

    public async Task RunAsync(DateOnly date, CancellationToken cancellationToken = default)
    {
        var options = optionsProvider.Create();

        using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeoutCts.CancelAfter(timeout ?? DefaultTimeout);
        var token = timeoutCts.Token;

        await using var agentFactory = factoryCreator(options);
        logger.LogInformation("Portfolio review: starting for {Date} (model: {Model})", date, options.Model);
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();

        try
        {
            var workflow = await workflowBuilder.BuildAsync(agentFactory, distributionSourceCreator(), token);

            await using var run = await InProcessExecution.RunStreamingAsync(workflow, new AnalysisRequest(date), cancellationToken: token);

            await foreach (var evt in run.WatchStreamAsync(token))
            {
                switch (evt)
                {
                    case AllocationRetrievedEvent or AssetDistributionRetrievedEvent or SynthesisWrittenEvent:
                        logger.LogInformation("Portfolio review: {Event} after {Seconds:0.0}s", evt, stopwatch.Elapsed.TotalSeconds);
                        break;

                    // The synthesis report is the workflow's only output: receiving it means the review is complete.
                    // It is not returned yet, the synthesis being a skeleton.
                    case WorkflowOutputEvent output when output.As<SynthesisReport>() is not null:
                        logger.LogInformation("Portfolio review: completed after {Seconds:0.0}s", stopwatch.Elapsed.TotalSeconds);
                        return;

                    // The framework reports executor failures as events rather than exceptions thrown to the caller.
                    case ExecutorFailedEvent failed:
                        throw Failed(failed.Data as Exception, $"step '{failed.ExecutorId}' failed");

                    case WorkflowErrorEvent error:
                        throw Failed(error.Data as Exception, "the workflow failed");
                }
            }

            // A cancelled run ends the event stream quietly instead of throwing: surface it as a cancellation or a timeout.
            token.ThrowIfCancellationRequested();
        }
        catch (HttpRequestException ex) when (ex.StatusCode == HttpStatusCode.Unauthorized)
        {
            // The MCP server is the only endpoint the workflow connects to while building its agents.
            throw new InvalidOperationException("The MCP key is missing or rejected: set it in the settings page.", ex);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            var limit = timeout ?? DefaultTimeout;
            logger.LogError("Portfolio review timed out after {Limit}", limit);
            throw new TimeoutException($"The analysis did not complete within {limit.TotalMinutes:0.#} min.");
        }

        throw new InvalidOperationException("The workflow ended without producing an output.");
    }

    private InvalidOperationException Failed(Exception? cause, string what)
    {
        logger.LogError(cause, "Portfolio review: {What}", what);
        return new InvalidOperationException(cause is null ? $"The analysis failed: {what}." : $"The analysis failed: {what} — {cause.Message}", cause);
    }
}

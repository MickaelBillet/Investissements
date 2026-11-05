using InvestissementsDashboard.Client.Model;
using InvestissementsDashboard.Client.Services;
using InvestissementsDashboard.Shared.Models;
using Microsoft.Extensions.Logging;

namespace InvestissementsDashboard.Client.ViewModels;

/// <summary>
/// Drives the portfolio review workflow from the analysis page and holds its progress and report. Scoped, so the report
/// outlives the page: coming back to it does not pay for a new analysis, as long as the application stays open.
/// </summary>
public class AnalysisViewModel(ILogger<AnalysisViewModel> logger, TimeProvider timeProvider, IPortfolioReviewRunner? runner = null)
{
    private CancellationTokenSource? _cts;

    /// <summary>Whether the analysis can run: only the MAUI host registers an <see cref="IPortfolioReviewRunner"/>.</summary>
    public bool IsAvailable => runner is not null;

    public bool IsRunning { get; private set; }
    public bool IsCancelled { get; private set; }
    public SynthesisReport? Report { get; private set; }
    public DateOnly? ReportDate { get; private set; }
    public string? Error { get; private set; }

    /// <summary>The state of every step, in display order. Replaced rather than mutated: the runner may update it from a background thread while the page reads it.</summary>
    public IReadOnlyList<AnalysisStepState> Steps { get; private set; } = PendingSteps();

    /// <summary>During a run, and after a failure that reached a step, so that the user sees which one failed.</summary>
    public bool ShowSteps => IsRunning || (Error is not null && Steps.Any(step => step.Status != AnalysisStepStatus.Pending));

    /// <summary>Raised when <see cref="Steps"/> changes, possibly on a background thread.</summary>
    public event Action? StepsChanged;

    public async Task LaunchAsync()
    {
        if (runner is null)
        {
            throw new InvalidOperationException("No IPortfolioReviewRunner is registered for this host.");
        }

        _cts?.Cancel();
        _cts?.Dispose();
        _cts = new CancellationTokenSource();

        IsRunning = true;
        IsCancelled = false;
        Report = null;
        ReportDate = null;
        Error = null;
        Steps = PendingSteps();
        try
        {
            var today = DateOnly.FromDateTime(timeProvider.GetLocalNow().DateTime);
            Report = await runner.RunAsync(today, new StepProgress(this), _cts.Token);
            ReportDate = today;
        }
        catch (OperationCanceledException)
        {
            // The user pressed Cancel or left the page: not a failure. Logged because the file log is the only trace of a run.
            logger.LogInformation("Portfolio review workflow cancelled");
            IsCancelled = true;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Portfolio review workflow failed");
            Error = ex.Message;
        }
        finally
        {
            IsRunning = false;
            // A step still running when another one failed will never finish: it must not keep spinning next to the error.
            Steps = [.. Steps.Select(step => step.Status == AnalysisStepStatus.Running ? step with { Status = AnalysisStepStatus.Pending } : step)];
        }
    }

    public void Cancel() => _cts?.Cancel();

    private static IReadOnlyList<AnalysisStepState> PendingSteps() =>
        [.. Enum.GetValues<AnalysisStep>().Select(step => new AnalysisStepState(step, AnalysisStepStatus.Pending))];

    private void Apply(AnalysisStepUpdate update)
    {
        var state = new AnalysisStepState(update.Step, update.Status);
        Steps = [.. Steps.Select(step => step.Step == update.Step ? state : step)];
        StepsChanged?.Invoke();
    }

    // Not Progress<T>: it posts each update for later, so one could land after the run's end and undo its last state.
    private sealed class StepProgress(AnalysisViewModel viewModel) : IProgress<AnalysisStepUpdate>
    {
        public void Report(AnalysisStepUpdate value) => viewModel.Apply(value);
    }
}

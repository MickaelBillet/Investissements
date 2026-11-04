using InvestissementsDashboard.Client.Services;
using InvestissementsDashboard.Shared.Models;
using Microsoft.Extensions.Logging;

namespace InvestissementsDashboard.Client.ViewModels;

/// <summary>
/// Drives the portfolio review workflow from the analysis page and holds its report. Scoped, so the report outlives
/// the page: coming back to it does not pay for a new analysis, as long as the application stays open.
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
        try
        {
            var today = DateOnly.FromDateTime(timeProvider.GetLocalNow().DateTime);
            Report = await runner.RunAsync(today, _cts.Token);
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
        }
    }

    public void Cancel() => _cts?.Cancel();
}

using InvestissementsDashboard.Client.Services;
using Microsoft.Extensions.Logging;

namespace InvestissementsDashboard.Client.ViewModels;

/// <summary>
/// Drives the portfolio review workflow from the analysis page. Only its completion is shown for now: the workflow
/// exposes no result while its synthesis step is a skeleton.
/// </summary>
public class AnalysisViewModel(ILogger<AnalysisViewModel> logger, TimeProvider timeProvider, IPortfolioReviewRunner? runner = null)
{
    private CancellationTokenSource? _cts;

    /// <summary>Whether the analysis can run: only the MAUI host registers an <see cref="IPortfolioReviewRunner"/>.</summary>
    public bool IsAvailable => runner is not null;

    public bool IsRunning { get; private set; }
    public bool IsCompleted { get; private set; }
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
        IsCompleted = false;
        Error = null;
        try
        {
            var today = DateOnly.FromDateTime(timeProvider.GetLocalNow().DateTime);
            await runner.RunAsync(today, _cts.Token);
            IsCompleted = true;
        }
        catch (OperationCanceledException)
        {
            // The user left the page while the workflow was running: nothing to display.
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

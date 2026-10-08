using InvestissementsDashboard.Client.Services;
using Microsoft.Extensions.Logging;

namespace InvestissementsDashboard.Client.ViewModels;

/// <summary>
/// Drives one agent run at a time (a single modal dialog is open at once, so state is reset on each launch).
/// The runner is optional (only the MAUI host registers it); whether the entry point is shown is decided by <see cref="AssetViewModel"/>.
/// </summary>
public class AgentViewModel(ILogger<AgentViewModel> logger, IAgentRunner? runner = null)
{
    private CancellationTokenSource? _cts;

    public bool IsRunning { get; private set; }
    public string? Response { get; private set; }
    public string? Error { get; private set; }

    public async Task LaunchAsync(AgentChoice agent, string assetName)
    {
        if (runner is null)
        {
            throw new InvalidOperationException("No IAgentRunner is registered for this host.");
        }

        _cts?.Cancel();
        _cts?.Dispose();
        _cts = new CancellationTokenSource();

        IsRunning = true;
        Response = null;
        Error = null;
        try
        {
            Response = await runner.RunAsync(agent, assetName, _cts.Token);
        }
        catch (OperationCanceledException)
        {
            // The user closed the dialog while the agent was running: nothing to display.
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Agent {Agent} failed for {Asset}", agent, assetName);
            Error = ex.Message;
        }
        finally
        {
            IsRunning = false;
        }
    }

    public void Cancel() => _cts?.Cancel();
}

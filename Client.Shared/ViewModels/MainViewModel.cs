using InvestissementsDashboard.Client.Services;
using Microsoft.Extensions.Logging;

namespace InvestissementsDashboard.Client.ViewModels;

/// <summary>
/// Presentation logic of <c>MainLayout</c>: the manual synchronization (and the message that reports its outcome), the logout
/// and whether the settings entry is shown (only when the host provides <see cref="IAgentSettings"/>, i.e. MAUI).
/// </summary>
public class MainViewModel(ISyncService syncService, ISessionService sessionService, ILocalizationService localizer, ILogger<MainViewModel> logger, IAgentSettings? agentSettings = null)
{
    public bool IsSettingsAvailable => agentSettings is not null;

    public bool IsSyncing { get; private set; }

    /// <summary>Localized outcome of the last synchronization, or <see langword="null"/> before the first one.</summary>
    public string? SyncMessage { get; private set; }

    public bool IsSyncSuccess { get; private set; }

    public Task LogoutAsync() => sessionService.LogoutAsync();

    public async Task SyncAsync(CancellationToken ct = default)
    {
        IsSyncing = true;
        try
        {
            var result = await syncService.TriggerAsync(ct);
            IsSyncSuccess = result.Success;
            SyncMessage = result.Success
                ? result.AddedCount > 0
                    ? string.Format(localizer.Translate("Sync_Success_WithAdded"), result.AddedCount)
                    : localizer.Translate("Sync_Success_NoAdded")
                : string.IsNullOrEmpty(result.ErrorMessage)
                    ? localizer.Translate("Sync_Error")
                    : $"{localizer.Translate("Sync_Error")} — {result.ErrorMessage}";
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogError(ex, "Manual synchronization failed");
            IsSyncSuccess = false;
            SyncMessage = $"{localizer.Translate("Sync_Error")} — {ex.Message}";
        }
        finally
        {
            IsSyncing = false;
        }
    }
}

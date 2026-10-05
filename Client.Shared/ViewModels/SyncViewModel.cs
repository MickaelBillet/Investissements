using InvestissementsDashboard.Client.Services;
using InvestissementsDashboard.Shared.Models;

namespace InvestissementsDashboard.Client.ViewModels;

public class SyncViewModel(ISyncService syncService)
{
    public bool IsSyncing { get; private set; }

    public async Task<SyncResultDto> TriggerAsync(CancellationToken ct = default)
    {
        IsSyncing = true;
        try
        {
            return await syncService.TriggerAsync(ct);
        }
        finally
        {
            IsSyncing = false;
        }
    }
}

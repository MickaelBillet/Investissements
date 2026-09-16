using InvestissementsDashboard.Shared.Models;

namespace InvestissementsDashboard.Client.Services;

public interface ISyncService
{
    Task<SyncResultDto> TriggerAsync(CancellationToken ct = default);
}

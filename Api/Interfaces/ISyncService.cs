using InvestissementsDashboard.Shared.Models;

namespace InvestissementsDashboard.Api.Services;

public interface ISyncService
{
    Task<SyncResultDto> TriggerAsync(CancellationToken ct = default);
}

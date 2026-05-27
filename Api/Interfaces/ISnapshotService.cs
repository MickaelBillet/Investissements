using InvestissementsDashboard.Shared.Models;

namespace InvestissementsDashboard.Api.Services;

public interface ISnapshotService
{
    Task<SnapshotDto?> GetLastAsync(CancellationToken ct = default);
    Task<IReadOnlyList<SnapshotDto>> GetHistoryAsync(CancellationToken ct = default);
}

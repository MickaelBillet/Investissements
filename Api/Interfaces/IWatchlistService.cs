using InvestissementsDashboard.Shared.Models;

namespace InvestissementsDashboard.Api.Services;

public interface IWatchlistService
{
    Task<IReadOnlyList<WatchlistItemDto>> GetAllAsync(CancellationToken ct = default);
}

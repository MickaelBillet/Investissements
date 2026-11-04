using InvestissementsDashboard.Shared.Models;

namespace InvestissementsDashboard.Api.Services;

public interface IBondScheduleService
{
    Task<IReadOnlyList<BondScheduleDto>> GetScheduleAsync(CancellationToken ct = default);
}

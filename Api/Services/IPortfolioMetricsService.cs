using InvestissementsDashboard.Shared.Models;

namespace InvestissementsDashboard.Api.Services;

public interface IPortfolioMetricsService
{
    Task<PortfolioMetricsDto>                GetMetricsAsync(CancellationToken ct = default);
    Task<IReadOnlyList<PerformancePointDto>> GetIndexedHistoryAsync(CancellationToken ct = default);
}

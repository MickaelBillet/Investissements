using InvestissementsDashboard.Shared.Models;

namespace InvestissementsDashboard.Api.Services;

public interface IGeographyService
{
    Task<IReadOnlyList<DistributionDto>> GetDistributionAsync(string assetClass, CancellationToken ct = default);
}

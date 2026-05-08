using InvestissementsDashboard.Shared.Models;

namespace InvestissementsDashboard.Api.Services;

public interface IAssetsService
{
    Task<IReadOnlyList<AssetDto>> GetAllAsync(CancellationToken ct = default);
    Task<IReadOnlyList<DistributionDto>> GetDistributionByDimensionAsync(string dimension, CancellationToken ct = default);
}

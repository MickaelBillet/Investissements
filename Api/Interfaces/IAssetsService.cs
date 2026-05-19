using InvestissementsDashboard.Shared.Models;

namespace InvestissementsDashboard.Api.Services;

public interface IAssetsService
{
    Task<IReadOnlyList<AssetDto>>       GetAllAsync(CancellationToken ct = default);
    Task<IReadOnlyList<DistributionDto>> GetDistributionByDimensionAsync(string dimension, CancellationToken ct = default);
    Task<IReadOnlyList<AggregateDto>>   GetEtfStocksByInformationAsync(CancellationToken ct = default);
    Task<IReadOnlyList<AssetDto>>       GetByAssetTypeAndInformationAsync(string assetType, string information, CancellationToken ct = default);
}

using InvestissementsDashboard.Shared.Models;

namespace InvestissementsDashboard.Api.Services;

internal sealed class SectorService(IAssetsService assetsService) : ISectorService
{
    public async Task<IReadOnlyList<DistributionDto>> GetDistributionAsync(
        string assetClass, CancellationToken ct = default)
    {
        var assetsTask   = assetsService.GetAllAsync(ct);
        var typeRefTask  = assetsService.GetAssetTypeReferenceAsync(ct);
        await Task.WhenAll(assetsTask, typeRefTask);

        var assets = await assetsTask;
        var eligibleAssetTypes = (await typeRefTask)
            .Where(t => t.GeoSectorEligible)
            .Select(t => t.Name)
            .ToHashSet();

        var sectorMap = new Dictionary<string, decimal>();

        foreach (var asset in assets)
        {
            if ((asset.AssetClass == assetClass) 
                && eligibleAssetTypes.Contains(asset.AssetType)
                && (string.IsNullOrWhiteSpace(asset.Sector) is false)
                && (asset.CurrentTotal is > 0m))
            {
                sectorMap.TryAdd(asset.Sector, 0m);
                sectorMap[asset.Sector] += asset.CurrentTotal.Value;
            }
        }

        var total = sectorMap.Values.Sum();

        return sectorMap
            .Select(kv => new DistributionDto(
                Id               : null,
                Name             : kv.Key,
                CurrentTotal     : Math.Round(kv.Value, 2),
                WeightInPortfolio: total > 0m
                    ? Math.Round(kv.Value / total * 100m, 2)
                    : 0m))
            .OrderByDescending(d => d.CurrentTotal)
            .ToArray();
    }
}

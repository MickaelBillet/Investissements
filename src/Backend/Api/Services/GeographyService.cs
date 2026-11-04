using InvestissementsDashboard.Shared;
using InvestissementsDashboard.Shared.Models;

namespace InvestissementsDashboard.Api.Services;

internal sealed class GeographyService(IAssetsService assetsService) : IGeographyService
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

        var zoneMap = new Dictionary<string, decimal>();

        foreach (var asset in assets)
        {
            if (asset.AssetClass != assetClass) continue;
            if (!eligibleAssetTypes.Contains(asset.AssetType)) continue;
            if (asset.CurrentTotal is not > 0m) continue;

            foreach (var (zone, pct) in ParseGeography(asset.Geography))
            {
                zoneMap.TryAdd(zone, 0m);
                zoneMap[zone] += asset.CurrentTotal.Value * pct;
            }
        }

        var total = zoneMap.Values.Sum();

        return zoneMap
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

    // Kept as a thin relay so existing tests (GeographyService.ParseGeography(...)) still compile —
    // actual parsing lives in Shared.GeographyParser, reused by the Client for the zone drill-down.
    internal static IEnumerable<(string Zone, decimal Pct)> ParseGeography(string geography) =>
        GeographyParser.Parse(geography);
}

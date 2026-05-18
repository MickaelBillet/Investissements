using System.Globalization;
using InvestissementsDashboard.Shared.Models;

namespace InvestissementsDashboard.Api.Services;

internal sealed class GeographyService(IAssetsService assetsService) : IGeographyService
{
    private static readonly HashSet<string> EligibleAssetTypes =
        ["Stock", "ETF_Stocks", "MarketBonds", "UnlistedBonds"];

    public async Task<IReadOnlyList<DistributionDto>> GetDistributionAsync(
        string assetClass, CancellationToken ct = default)
    {
        var assets = await assetsService.GetAllAsync(ct);

        var zoneMap = new Dictionary<string, decimal>();

        foreach (var asset in assets)
        {
            if (asset.AssetClass != assetClass) continue;
            if (!EligibleAssetTypes.Contains(asset.AssetType)) continue;
            if (asset.CurrentTotal is not > 0m) continue;

            foreach (var (zone, pct) in ParseGeography(asset.Geography))
            {
                zoneMap.TryAdd(zone, 0m);
                zoneMap[zone] += asset.CurrentTotal.Value * pct;
            }
        }

        var total = zoneMap.Values.Sum();

        return [.. zoneMap
            .Select(kv => new DistributionDto(
                Id               : null,
                Name             : kv.Key,
                CurrentTotal     : Math.Round(kv.Value, 2),
                WeightInPortfolio: total > 0m
                    ? Math.Round(kv.Value / total * 100m, 2)
                    : 0m))
            .OrderByDescending(d => d.CurrentTotal)];
    }

    // Parse "Zone1 : X% - Zone2 : Y%" → (zone, pct) pairs
    internal static IEnumerable<(string Zone, decimal Pct)> ParseGeography(string geography)
    {
        if (string.IsNullOrWhiteSpace(geography)) yield break;

        foreach (var part in geography.Split(" - "))
        {
            var sepIdx = part.LastIndexOf(" : ");
            if (sepIdx == -1) continue;

            var zone   = part[..sepIdx].Trim();
            var pctStr = part[(sepIdx + 3)..].Replace("%", "").Trim();

            if (!string.IsNullOrEmpty(zone)
                && decimal.TryParse(pctStr, NumberStyles.Number, CultureInfo.InvariantCulture, out var pct))
                yield return (zone, pct / 100m);
        }
    }
}

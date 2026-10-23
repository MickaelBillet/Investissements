using InvestissementsDashboard.Shared;
using InvestissementsDashboard.Shared.Models;

namespace InvestissementsDashboard.Client.Utils;

/// <summary>
/// Assets behind a slice of the geography or sector donut. Uses the same eligibility filter as the Api
/// (GeographyService, SectorService) so the list's total matches the donut computed server-side.
/// </summary>
public static class GeographySectorFilter
{
    public static IReadOnlyList<AssetDto> AssetsForZone(
        IEnumerable<AssetDto> assets, IReadOnlyList<AssetTypeReferenceDto> assetTypes, string assetClass, string zone) =>
        ZoneMatches(assets, assetTypes, assetClass, zone)
            .Select(m => m.Asset)
            .OrderByDescending(a => a.CurrentTotal)
            .ToList();

    // An ETF can span several zones (e.g. "USA : 41% - Europe : 24%"), so the zone's true weight is
    // CurrentTotal × coefficient, not the asset's full CurrentTotal.
    public static IReadOnlyDictionary<int, decimal> ZoneCoefficients(
        IEnumerable<AssetDto> assets, IReadOnlyList<AssetTypeReferenceDto> assetTypes, string assetClass, string zone) =>
        ZoneMatches(assets, assetTypes, assetClass, zone).ToDictionary(m => m.Asset.Id, m => m.Coefficient);

    public static IReadOnlyList<AssetDto> AssetsForSector(
        IEnumerable<AssetDto> assets, IReadOnlyList<AssetTypeReferenceDto> assetTypes, string assetClass, string sector)
    {
        var eligibleTypes = EligibleTypes(assetTypes);
        return assets
            .Where(a => a.AssetClass == assetClass && eligibleTypes.Contains(a.AssetType) && a.Sector == sector)
            .OrderByDescending(a => a.CurrentTotal)
            .ToList();
    }

    private static IEnumerable<(AssetDto Asset, decimal Coefficient)> ZoneMatches(
        IEnumerable<AssetDto> assets, IReadOnlyList<AssetTypeReferenceDto> assetTypes, string assetClass, string zone)
    {
        var eligibleTypes = EligibleTypes(assetTypes);
        return assets
            .Where(a => a.AssetClass == assetClass && eligibleTypes.Contains(a.AssetType))
            .SelectMany(a => GeographyParser.Parse(a.Geography)
                .Where(z => z.Zone == zone)
                .Select(z => (Asset: a, Coefficient: z.Pct)));
    }

    private static HashSet<string> EligibleTypes(IReadOnlyList<AssetTypeReferenceDto> assetTypes) =>
        assetTypes.Where(r => r.GeoSectorEligible).Select(r => r.Name).ToHashSet();
}

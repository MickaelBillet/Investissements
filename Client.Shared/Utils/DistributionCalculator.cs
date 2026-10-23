using InvestissementsDashboard.Client.Model;
using InvestissementsDashboard.Shared.Models;

namespace InvestissementsDashboard.Client.Utils;

/// <summary>Groups assets into donut slices: value per group and its weight in the given assets' total.</summary>
public static class DistributionCalculator
{
    // groupBy picks the property to group on (e.g. a => a.AssetClass); toLabel optionally turns a group name into
    // its displayed label (e.g. a translation). Callers pass functions so one method serves every donut.
    public static IReadOnlyList<DistributionItem> Compute(
        IEnumerable<AssetDto> assets,
        Func<AssetDto, string> groupBy,
        Func<string, string>? toLabel = null)
    {
        // Materialized once: the assets are enumerated for the total, then again for the groups.
        var assetList = assets.ToList();
        var total     = assetList.Sum(a => a.CurrentTotal ?? 0);

        var items = new List<DistributionItem>();
        foreach (var group in assetList.GroupBy(groupBy))
        {
            var name       = group.Key;
            var label      = toLabel is null ? name : toLabel(name);
            var groupTotal = group.Sum(a => a.CurrentTotal ?? 0);
            var weight     = total > 0 ? groupTotal / total * 100m : 0m;

            items.Add(new DistributionItem(name, label, groupTotal, weight));
        }

        return items.OrderByDescending(i => i.CurrentTotal).ToList();
    }

    // Distributions already computed by the Api (geography, sectors): only the label is left to decide.
    public static IReadOnlyList<DistributionItem> FromApi(
        IEnumerable<DistributionDto> distribution,
        Func<string, string>? toLabel = null) =>
        distribution
            .Select(d => new DistributionItem(d.Name, toLabel is null ? d.Name : toLabel(d.Name), d.CurrentTotal, d.WeightInPortfolio))
            .ToList();
}

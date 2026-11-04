using InvestissementsDashboard.Client.Services;
using InvestissementsDashboard.Shared.Constants;
using InvestissementsDashboard.Shared.Models;

namespace InvestissementsDashboard.Client.ViewModels;

/// <summary>
/// Presentation rules for <c>AssetTable</c>. The agent runner is optional: only the MAUI host registers it,
/// so the WASM site reports <see cref="IsAgentAvailable"/> = false and the table shows no agent entry point.
/// </summary>
public class AssetViewModel(IAgentRunner? runner = null)
{
    public bool IsAgentAvailable => runner is not null;

    // Agents only analyse individual stocks: funds, bonds and savings have no company to look up.
    public bool CanLaunchAgent(AssetDto asset)
    {
        ArgumentNullException.ThrowIfNull(asset);
        return IsAgentAvailable && asset.AssetType == AssetTypeNames.Stock;
    }

    // With zone coefficients (geographic drill-down) an asset only counts for its share in the zone,
    // so the footer total is weighted instead of a raw sum.
    public decimal GetTotal(IReadOnlyList<AssetDto> assets, IReadOnlyDictionary<int, decimal>? coefficients = null)
    {
        ArgumentNullException.ThrowIfNull(assets);

        return coefficients is null
            ? assets.Sum(a => a.CurrentTotal ?? 0m)
            : assets.Sum(a => (a.CurrentTotal ?? 0m) * (coefficients.TryGetValue(a.Id, out var c) ? c : 0m));
    }
}

using InvestissementsDashboard.Client.Model;
using InvestissementsDashboard.Shared.Models;

namespace InvestissementsDashboard.Client.Services;

/// <summary>
/// What a drill-down panel shows at its current level: the donut slices, or the assets once the leaf level is reached.
/// Stateless: the caller passes the active assets and the asset type reference loaded from the Api.
/// </summary>
public interface IPanelDistributionService
{
    IReadOnlyList<DistributionItem> GetDistribution(
        IEnumerable<AssetDto> activeAssets, IReadOnlyList<AssetTypeReferenceDto> assetTypes, PanelState panel, bool groupEtfByInformation);

    IReadOnlyList<AssetDto> GetLeafAssets(IEnumerable<AssetDto> activeAssets, PanelState panel, bool groupEtfByInformation);

    /// <summary>French label from the Sheet's AssetType tab, falling back to the raw name.</summary>
    string TranslateAssetType(IReadOnlyList<AssetTypeReferenceDto> assetTypes, string assetType);
}

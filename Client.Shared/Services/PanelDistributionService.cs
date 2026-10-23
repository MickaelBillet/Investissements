using InvestissementsDashboard.Client.Model;
using InvestissementsDashboard.Client.Utils;
using InvestissementsDashboard.Shared.Constants;
using InvestissementsDashboard.Shared.Models;

namespace InvestissementsDashboard.Client.Services;

public class PanelDistributionService(ILocalizationService localizationService) : IPanelDistributionService
{
    public IReadOnlyList<DistributionItem> GetDistribution(
        IEnumerable<AssetDto> activeAssets, IReadOnlyList<AssetTypeReferenceDto> assetTypes, PanelState panel, bool groupEtfByInformation) =>
        panel.Type switch
        {
            PanelType.AssetClass  => GetAssetClassDistribution(activeAssets, assetTypes, panel, groupEtfByInformation),
            PanelType.SupportType => GetSupportTypeDistribution(activeAssets, panel),
            PanelType.Risk        => GetRiskDistribution(activeAssets, panel),
            _                     => []
        };

    public IReadOnlyList<AssetDto> GetLeafAssets(IEnumerable<AssetDto> activeAssets, PanelState panel, bool groupEtfByInformation)
    {
        if (!panel.IsAtLeafLevel) return [];

        // Grouped ETFs add a level: the assets are then those of the selected information group.
        var information = IsEtfGrouped(panel, groupEtfByInformation) ? panel.Selected(2) : null;

        var leafAssets = panel.Type switch
        {
            PanelType.AssetClass  => activeAssets.Where(a => a.AssetClass == panel.Selected(0)
                                                          && a.AssetType == panel.Selected(1)
                                                          && (information == null || a.Information == information)),
            PanelType.SupportType => activeAssets.Where(a => a.Support == panel.Selected(1)),
            PanelType.Risk        => activeAssets.Where(a => a.Risk.ToString() == panel.Selected(0)),
            _                     => []
        };

        return leafAssets.OrderByDescending(a => a.CurrentTotal).ToList();
    }

    public string TranslateAssetType(IReadOnlyList<AssetTypeReferenceDto> assetTypes, string assetType) =>
        assetTypes.FirstOrDefault(r => r.Name == assetType)?.LabelFr ?? assetType;

    private static bool IsEtfGrouped(PanelState panel, bool groupEtfByInformation) =>
        groupEtfByInformation && panel.Selected(1) == AssetTypeNames.EtfStocks;

    private IReadOnlyList<DistributionItem> GetAssetClassDistribution(
        IEnumerable<AssetDto> assets, IReadOnlyList<AssetTypeReferenceDto> assetTypes, PanelState panel, bool groupEtfByInformation)
    {
        var assetClass = panel.Selected(0);
        return panel.Level switch
        {
            0 => DistributionCalculator.Compute(assets, a => a.AssetClass, localizationService.Translate),
            1 => DistributionCalculator.Compute(
                    assets.Where(a => a.AssetClass == assetClass),
                    a => a.AssetType,
                    assetType => TranslateAssetType(assetTypes, assetType)),
            2 when IsEtfGrouped(panel, groupEtfByInformation)
              => DistributionCalculator.Compute(
                    assets.Where(a => a.AssetClass == assetClass && a.AssetType == AssetTypeNames.EtfStocks),
                    a => a.Information),
            2 => DistributionCalculator.Compute(
                    assets.Where(a => a.AssetClass == assetClass && a.AssetType == panel.Selected(1)),
                    a => a.Name),
            _ => DistributionCalculator.Compute(
                    assets.Where(a => a.AssetClass == assetClass
                                   && a.AssetType == AssetTypeNames.EtfStocks
                                   && a.Information == panel.Selected(2)),
                    a => a.Name)
        };
    }

    private IReadOnlyList<DistributionItem> GetSupportTypeDistribution(IEnumerable<AssetDto> assets, PanelState panel) =>
        panel.Level switch
        {
            0 => DistributionCalculator.Compute(assets, a => a.SupportType, localizationService.Translate),
            1 => DistributionCalculator.Compute(assets.Where(a => a.SupportType == panel.Selected(0)), a => a.Support),
            _ => DistributionCalculator.Compute(
                    assets.Where(a => a.SupportType == panel.Selected(0) && a.Support == panel.Selected(1)),
                    a => a.Name)
        };

    private IReadOnlyList<DistributionItem> GetRiskDistribution(IEnumerable<AssetDto> assets, PanelState panel) =>
        panel.Level switch
        {
            0 => DistributionCalculator.Compute(assets, a => a.Risk.ToString(), risk => localizationService.Translate($"Risk_{risk}")),
            _ => DistributionCalculator.Compute(assets.Where(a => a.Risk.ToString() == panel.Selected(0)), a => a.Name)
        };
}

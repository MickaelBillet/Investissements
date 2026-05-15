using InvestissementsDashboard.Client.Model;
using InvestissementsDashboard.Client.Services;
using InvestissementsDashboard.Shared.Models;

namespace InvestissementsDashboard.Client.ViewModels;

public class DashboardViewModel(IPortfolioService portfolioService)
{
    private IReadOnlyList<AssetDto> _assets = [];

    public SnapshotDto? LastSnapshot  { get; private set; }
    public bool         IsLoading     { get; private set; } = true;
    public string?      ErrorMessage  { get; private set; }
    public int          AssetCount    => _assets.Count;

    public PanelState AssetClassPanel  { get; } = new(PanelType.AssetClass);
    public PanelState SupportTypePanel { get; } = new(PanelType.SupportType);
    public PanelState RiskPanel        { get; } = new(PanelType.Risk);

    public bool EtfStocksGroupByInformation { get; set; }

    public decimal? PortfolioRoiOnCapitalEngaged { get; private set; }
    public decimal? PortfolioRoiOnTotalPurchases { get; private set; }

    public async Task InitializeAsync(CancellationToken ct = default)
    {
        if (!IsLoading && _assets.Count > 0) return;

        IsLoading    = true;
        ErrorMessage = null;
        try
        {
            var assetsTask   = portfolioService.GetAssetsAsync(ct);
            var snapshotTask = portfolioService.GetLastSnapshotAsync(ct);
            await Task.WhenAll(assetsTask, snapshotTask);
            _assets      = await assetsTask;
            LastSnapshot = await snapshotTask;
            ComputePortfolioRoi();
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Impossible de charger les données : {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    public bool IsLeafLevel(PanelState panel)
    {
        if (panel.Type == PanelType.AssetClass
            && EtfStocksGroupByInformation
            && panel.Selected(1) == "ETF_Stocks")
            return panel.Level >= 3;

        return panel.Type == PanelType.Risk ? panel.Level >= 1 : panel.Level >= 2;
    }

    public IReadOnlyList<DistributionItem> GetDistribution(PanelState panel) =>
        panel.Type switch
        {
            PanelType.AssetClass  => GetAssetClassDistribution(panel),
            PanelType.SupportType => GetSupportTypeDistribution(panel),
            PanelType.Risk        => GetRiskDistribution(panel),
            _                     => []
        };

    public IReadOnlyList<AssetDto> GetAssetsForPanel(PanelState panel)
    {
        if (!panel.IsAtLeafLevel) return [];
        return panel.Type switch
        {
            PanelType.AssetClass  => GetLeafAssetsForAssetClass(
                panel.Selected(0)!,
                panel.Selected(1)!,
                EtfStocksGroupByInformation && panel.Selected(1) == "ETF_Stocks" ? panel.Selected(2) : null),
            PanelType.SupportType => GetLeafAssetsForSupport(panel.Selected(1)!),
            PanelType.Risk        => GetLeafAssetsForRisk(panel.Selected(0)!),
            _                     => []
        };
    }

    private IReadOnlyList<DistributionItem> GetAssetClassDistribution(PanelState panel)
    {
        var isEtfGrouped = EtfStocksGroupByInformation && panel.Selected(1) == "ETF_Stocks";

        return panel.Level switch
        {
            0 => ComputeDistribution(ActiveAssets(), a => a.AssetClass),
            1 => ComputeDistribution(ActiveAssets().Where(a => a.AssetClass == panel.Selected(0)), a => a.AssetType),
            2 when isEtfGrouped
              => ComputeDistribution(
                    ActiveAssets().Where(a => a.AssetClass == panel.Selected(0) && a.AssetType == "ETF_Stocks"),
                    a => a.Information),
            2 => ComputeDistribution(
                    ActiveAssets().Where(a => a.AssetClass == panel.Selected(0) && a.AssetType == panel.Selected(1)),
                    a => a.Name),
            _ => ComputeDistribution(
                    ActiveAssets().Where(a => a.AssetClass == panel.Selected(0)
                                           && a.AssetType == "ETF_Stocks"
                                           && a.Information == panel.Selected(2)),
                    a => a.Name)
        };
    }

    private IReadOnlyList<DistributionItem> GetSupportTypeDistribution(PanelState panel) =>
        panel.Level switch
        {
            0 => ComputeDistribution(ActiveAssets(), a => a.SupportType),
            1 => ComputeDistribution(ActiveAssets().Where(a => a.SupportType == panel.Selected(0)), a => a.Support),
            _ => ComputeDistribution(ActiveAssets().Where(a => a.SupportType == panel.Selected(0) && a.Support == panel.Selected(1)), a => a.Name)
        };

    private IReadOnlyList<DistributionItem> GetRiskDistribution(PanelState panel) =>
        panel.Level switch
        {
            0 => ComputeDistribution(ActiveAssets(), a => a.Risk.ToString()),
            _ => ComputeDistribution(ActiveAssets().Where(a => a.Risk.ToString() == panel.Selected(0)), a => a.Name)
        };

    private IReadOnlyList<AssetDto> GetLeafAssetsForAssetClass(string assetClass, string assetType, string? information = null) =>
        [.. ActiveAssets()
             .Where(a => a.AssetClass == assetClass
                      && a.AssetType == assetType
                      && (information == null || a.Information == information))
             .OrderByDescending(a => a.CurrentTotal)];

    private IReadOnlyList<AssetDto> GetLeafAssetsForSupport(string support) =>
        [.. ActiveAssets()
             .Where(a => a.Support == support)
             .OrderByDescending(a => a.CurrentTotal)];

    private IReadOnlyList<AssetDto> GetLeafAssetsForRisk(string risk) =>
        [.. ActiveAssets()
             .Where(a => a.Risk.ToString() == risk)
             .OrderByDescending(a => a.CurrentTotal)];

    private IEnumerable<AssetDto> ActiveAssets() =>
        _assets.Where(a => a.CurrentTotal is > 0);

    private void ComputePortfolioRoi()
    {
        if (LastSnapshot?.TotalPurchases is not > 0m) return;

        var purchases      = LastSnapshot.TotalPurchases!.Value;
        var returns        = LastSnapshot.TotalReturns ?? 0m;
        var gain           = LastSnapshot.PortfolioTotal + returns - purchases;
        var capitalEngaged = purchases - returns;

        PortfolioRoiOnTotalPurchases = gain / purchases * 100m;
        if (capitalEngaged > 0m)
            PortfolioRoiOnCapitalEngaged = gain / capitalEngaged * 100m;
    }

    private static IReadOnlyList<DistributionItem> ComputeDistribution(
        IEnumerable<AssetDto> assets,
        Func<AssetDto, string> groupKey)
    {
        var list  = assets.ToList();
        var total = list.Sum(a => a.CurrentTotal ?? 0);
        return [.. list
            .GroupBy(groupKey)
            .Select(g =>
            {
                var groupTotal = g.Sum(a => a.CurrentTotal ?? 0);
                return new DistributionItem(
                    g.Key,
                    groupTotal,
                    total > 0 ? groupTotal / total * 100m : 0m);
            })
            .OrderByDescending(d => d.CurrentTotal)];
    }
}

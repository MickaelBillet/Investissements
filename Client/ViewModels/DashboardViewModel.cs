using InvestissementsDashboard.Client.Model;
using InvestissementsDashboard.Client.Services;
using InvestissementsDashboard.Shared.Models;

namespace InvestissementsDashboard.Client.ViewModels;

public class DashboardViewModel(IPortfolioService portfolioService, ILocalizationService localizationService)
{
    private IReadOnlyList<AssetDto>        _assets           = [];
    private IReadOnlyList<DistributionDto> _geoStocks        = [];
    private IReadOnlyList<DistributionDto> _geoBonds         = [];
    private PortfolioMetricsDto?           _metrics;
    private IReadOnlyList<SnapshotDto>     _snapshotHistory  = [];

    public SnapshotDto? LastSnapshot  { get; private set; }
    public bool         IsLoading     { get; private set; } = true;
    public string?      ErrorMessage  { get; private set; }
    public int          AssetCount    => ActiveAssets().Count();

    public PanelState AssetClassPanel  { get; } = new(PanelType.AssetClass);
    public PanelState SupportTypePanel { get; } = new(PanelType.SupportType);
    public PanelState RiskPanel        { get; } = new(PanelType.Risk);

    public bool EtfStocksGroupByInformation { get; set; }

    public decimal? PortfolioRoiOnCapitalEngaged => _metrics?.RoiOnCapitalEngaged;
    public decimal? PortfolioRoiOnTotalPurchases => _metrics?.RoiOnTotalPurchases;
    public decimal? AverageRisk                  => _metrics?.AverageRisk;
    public decimal? DailyVariationPercent                => ComputeVariation(_snapshotHistory, 1);
    public decimal? WeeklyVariationPercent               => ComputeVariation(_snapshotHistory, 7);
    public decimal? DailyROICapitalEngagedVariation      => ComputeROIVariation(_snapshotHistory, 1,  s => s.PortfolioTotal > 0 ? s.TotalReturns / s.PortfolioTotal  * 100m : null);
    public decimal? WeeklyROICapitalEngagedVariation     => ComputeROIVariation(_snapshotHistory, 7,  s => s.PortfolioTotal > 0 ? s.TotalReturns / s.PortfolioTotal  * 100m : null);
    public decimal? DailyROITotalPurchasesVariation      => ComputeROIVariation(_snapshotHistory, 1,  s => s.TotalPurchases  > 0 ? s.TotalReturns / s.TotalPurchases  * 100m : null);
    public decimal? WeeklyROITotalPurchasesVariation     => ComputeROIVariation(_snapshotHistory, 7,  s => s.TotalPurchases  > 0 ? s.TotalReturns / s.TotalPurchases  * 100m : null);

    public async Task InitializeAsync(CancellationToken ct = default)
    {
        if (!IsLoading && _assets.Count > 0) return;

        IsLoading    = true;
        ErrorMessage = null;
        try
        {
            static Task<IReadOnlyList<DistributionDto>> SafeGeo(Task<IReadOnlyList<DistributionDto>> t) =>
                t.ContinueWith(r => r.IsCompletedSuccessfully ? r.Result : (IReadOnlyList<DistributionDto>)[],
                               TaskScheduler.Default);

            var assetsTask    = portfolioService.GetAssetsAsync(ct);
            var snapshotTask  = portfolioService.GetLastSnapshotAsync(ct);
            var metricsTask   = portfolioService.GetMetricsAsync(ct);
            var historyTask   = portfolioService.GetSnapshotHistoryAsync(ct);
            var geoStocksTask = SafeGeo(portfolioService.GetGeographyDistributionAsync("Stocks", ct));
            var geoBondsTask  = SafeGeo(portfolioService.GetGeographyDistributionAsync("Bonds", ct));
            await Task.WhenAll(assetsTask, snapshotTask, metricsTask, historyTask, geoStocksTask, geoBondsTask);
            _assets          = await assetsTask;
            LastSnapshot     = await snapshotTask;
            _metrics         = await metricsTask;
            _snapshotHistory = await historyTask;
            _geoStocks       = await geoStocksTask;
            _geoBonds        = await geoBondsTask;
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

    public string GetPanelTitle(PanelState panel)
    {
        if (panel.Level == 0) return panel.BreadcrumbLabel;

        var segments = Enumerable.Range(0, panel.Level)
            .Select(i => panel.Selected(i)!)
            .Select(key => panel.Type == PanelType.Risk
                ? localizationService.Translate($"Risk_{key}")
                : localizationService.Translate(key));

        return string.Join(" › ", segments);
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

    // --- Geography (pre-loaded from API, filtered client-side for zone drill-down) ---

    public IReadOnlyList<DistributionItem> GetGeographyForClass(string assetClass)
    {
        var distribution = assetClass == "Stocks" ? _geoStocks : _geoBonds;
        return [.. distribution.Select(d => new DistributionItem(d.Name, d.Name, d.CurrentTotal, d.WeightInPortfolio))];
    }

    public IReadOnlyList<AssetDto> GetAssetsForZone(string assetClass, string zone) =>
        [.. ActiveAssets()
             .Where(a => a.AssetClass == assetClass && a.Geography.Contains(zone))
             .OrderByDescending(a => a.CurrentTotal)];

    private static readonly HashSet<string> GeoAndSectorEligibleTypes =
        ["Stock", "ETF_Stocks", "MarketBonds", "UnlistedBonds"];

    public IReadOnlyList<DistributionItem> GetSectorForClass(string assetClass) =>
        ComputeDistribution(
            ActiveAssets().Where(a => a.AssetClass == assetClass
                                   && GeoAndSectorEligibleTypes.Contains(a.AssetType)
                                   && !string.IsNullOrEmpty(a.Sector)),
            a => a.Sector,
            localizationService.Translate);

    public IReadOnlyList<AssetDto> GetAssetsForSector(string assetClass, string sector) =>
        [.. ActiveAssets()
             .Where(a => a.AssetClass == assetClass && a.Sector == sector)
             .OrderByDescending(a => a.CurrentTotal)];

    // --- Private distribution methods ---

    private IReadOnlyList<DistributionItem> GetAssetClassDistribution(PanelState panel)
    {
        var isEtfGrouped = EtfStocksGroupByInformation && panel.Selected(1) == "ETF_Stocks";

        return panel.Level switch
        {
            0 => ComputeDistribution(ActiveAssets(), a => a.AssetClass, localizationService.Translate),
            1 => ComputeDistribution(ActiveAssets().Where(a => a.AssetClass == panel.Selected(0)), a => a.AssetType, localizationService.Translate),
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
            0 => ComputeDistribution(ActiveAssets(), a => a.SupportType, localizationService.Translate),
            1 => ComputeDistribution(ActiveAssets().Where(a => a.SupportType == panel.Selected(0)), a => a.Support),
            _ => ComputeDistribution(ActiveAssets().Where(a => a.SupportType == panel.Selected(0) && a.Support == panel.Selected(1)), a => a.Name)
        };

    private IReadOnlyList<DistributionItem> GetRiskDistribution(PanelState panel) =>
        panel.Level switch
        {
            0 => ComputeDistribution(ActiveAssets(), a => a.Risk.ToString(), k => localizationService.Translate($"Risk_{k}")),
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

    private static decimal? ComputeVariation(IReadOnlyList<SnapshotDto> history, int daysBack)
    {
        if (history.Count < 2) return null;
        var last      = history[^1];
        var reference = daysBack == 1
            ? history[^2]
            : history.LastOrDefault(s => s.Date <= last.Date.AddDays(-daysBack));
        if (reference is null || reference.PortfolioTotal == 0) return null;
        return (last.PortfolioTotal - reference.PortfolioTotal) / reference.PortfolioTotal * 100m;
    }

    private static decimal? ComputeROIVariation(IReadOnlyList<SnapshotDto> history, int daysBack, Func<SnapshotDto, decimal?> roiOf)
    {
        if (history.Count < 2) return null;
        var last      = history[^1];
        var reference = daysBack == 1
            ? history[^2]
            : history.LastOrDefault(s => s.Date <= last.Date.AddDays(-daysBack));
        if (reference is null) return null;
        var roiLast = roiOf(last);
        var roiRef  = roiOf(reference);
        if (!roiLast.HasValue || !roiRef.HasValue || roiRef.Value == 0) return null;
        return (roiLast.Value - roiRef.Value) / Math.Abs(roiRef.Value) * 100m;
    }

    private static IReadOnlyList<DistributionItem> ComputeDistribution(
        IEnumerable<AssetDto> assets,
        Func<AssetDto, string> groupKey,
        Func<string, string>? translateKey = null)
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
                    translateKey?.Invoke(g.Key) ?? g.Key,
                    groupTotal,
                    total > 0 ? groupTotal / total * 100m : 0m);
            })
            .OrderByDescending(d => d.CurrentTotal)];
    }
}

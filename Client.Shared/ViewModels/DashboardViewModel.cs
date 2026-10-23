using InvestissementsDashboard.Client.Model;
using InvestissementsDashboard.Client.Services;
using InvestissementsDashboard.Client.Utils;
using InvestissementsDashboard.Shared.Constants;
using InvestissementsDashboard.Shared.Models;
using Microsoft.Extensions.Logging;

namespace InvestissementsDashboard.Client.ViewModels;

public class DashboardViewModel(IPortfolioService portfolioService, ILocalizationService localizationService,
    IPanelDistributionService panelDistributionService, ILogger<DashboardViewModel> logger)
{
    private IReadOnlyList<AssetDto>        _assets           = [];
    private IReadOnlyList<DistributionDto> _geoStocks        = [];
    private IReadOnlyList<DistributionDto> _geoBonds         = [];
    private IReadOnlyList<DistributionDto> _sectorStocks     = [];
    private IReadOnlyList<DistributionDto> _sectorBonds      = [];
    private PortfolioMetricsDto?           _metrics;
    private IReadOnlyList<SnapshotDto>     _snapshotHistory  = [];
    private IReadOnlyList<PerformancePointDto> _performanceHistory = [];
    private IReadOnlyList<AssetTypeReferenceDto> _assetTypeRef = [];

    public SnapshotDto? LastSnapshot  { get; private set; }
    public bool         IsLoading     { get; private set; } = true;
    public string?      ErrorMessage  { get; private set; }
    public int          AssetCount    => ActiveAssets().Count();

    public PanelState AssetClassPanel  { get; } = new(PanelType.AssetClass);
    public PanelState SupportTypePanel { get; } = new(PanelType.SupportType);
    public PanelState RiskPanel        { get; } = new(PanelType.Risk);

    // Only one hierarchy can be drilled into at a time (a drill-down starts from the overview), so the
    // active panel is simply the one that has left level 0: no separate "active hierarchy" state to keep in sync.
    public PanelState? ActivePanel =>
        AssetClassPanel.CanGoBack  ? AssetClassPanel  :
        SupportTypePanel.CanGoBack ? SupportTypePanel :
        RiskPanel.CanGoBack        ? RiskPanel        : null;

    public bool HasActivePanel => ActivePanel is not null;

    public string? SelectedZone   { get; private set; }
    public string? SelectedSector { get; private set; }

    // The geography/sector donuts only make sense right under a Stocks or Bonds asset class.
    public string? GeoClass =>
        ActivePanel is { Type: PanelType.AssetClass, Level: 1 } panel
        && panel.Selected(0) is AssetClassNames.Stocks or AssetClassNames.Bonds
            ? panel.Selected(0)
            : null;

    public bool ShowEtfGroupingToggle =>
        ActivePanel is { Type: PanelType.AssetClass } panel && panel.Selected(1) == AssetTypeNames.EtfStocks;

    public bool EtfStocksGroupByInformation { get; set; }

    public decimal? PortfolioRoiOnCapitalEngaged => _metrics?.RoiOnCapitalEngaged;
    public decimal? AverageRisk                  => _metrics?.AverageRisk;
    public decimal? DailyVariationPercent                => VariationCalculator.SinceDaysBack(_snapshotHistory, 1);
    public decimal? WeeklyVariationPercent               => VariationCalculator.SinceDaysBack(_snapshotHistory, 7);
    public decimal? MonthlyVariationPercent              => VariationCalculator.SinceDaysBack(_snapshotHistory, 30);
    public decimal? YtdVariationPercent                  => VariationCalculator.SinceYearStart(_snapshotHistory);
    public decimal? YearlyVariationPercent               => VariationCalculator.SinceDaysBack(_snapshotHistory, 365);

    public decimal? DailyROICapitalEngagedVariation      => VariationCalculator.SinceDaysBack(_performanceHistory, 1);
    public decimal? WeeklyROICapitalEngagedVariation     => VariationCalculator.SinceDaysBack(_performanceHistory, 7);
    public decimal? MonthlyROICapitalEngagedVariation    => VariationCalculator.SinceDaysBack(_performanceHistory, 30);
    public decimal? YtdROICapitalEngagedVariation        => VariationCalculator.SinceYearStart(_performanceHistory);
    public decimal? YearlyROICapitalEngagedVariation     => VariationCalculator.SinceDaysBack(_performanceHistory, 365);

    public async Task InitializeAsync(CancellationToken ct = default)
    {
        if (!IsLoading && _assets.Count > 0) return;

        IsLoading    = true;
        ErrorMessage = null;
        try
        {
            var assetsTask      = portfolioService.GetAssetsAsync(ct);
            var snapshotTask    = portfolioService.GetLastSnapshotAsync(ct);
            var metricsTask     = portfolioService.GetMetricsAsync(ct);
            var historyTask     = portfolioService.GetSnapshotHistoryAsync(ct);
            var performanceTask = portfolioService.GetIndexedHistoryAsync(ct);
            var geoStocksTask    = LoadDistributionAsync(portfolioService.GetGeographyDistributionAsync, "Geography", AssetClassNames.Stocks, ct);
            var geoBondsTask     = LoadDistributionAsync(portfolioService.GetGeographyDistributionAsync, "Geography", AssetClassNames.Bonds, ct);
            var sectorStocksTask = LoadDistributionAsync(portfolioService.GetSectorDistributionAsync, "Sector", AssetClassNames.Stocks, ct);
            var sectorBondsTask  = LoadDistributionAsync(portfolioService.GetSectorDistributionAsync, "Sector", AssetClassNames.Bonds, ct);
            var assetTypeRefTask = portfolioService.GetAssetTypeReferenceAsync(ct);
            await Task.WhenAll(assetsTask, snapshotTask, metricsTask, historyTask, performanceTask,
                               geoStocksTask, geoBondsTask, sectorStocksTask, sectorBondsTask, assetTypeRefTask);
            _assets             = await assetsTask;
            LastSnapshot        = await snapshotTask;
            _metrics            = await metricsTask;
            _snapshotHistory    = await historyTask;
            _performanceHistory = await performanceTask;
            _geoStocks          = await geoStocksTask;
            _geoBonds           = await geoBondsTask;
            _sectorStocks       = await sectorStocksTask;
            _sectorBonds        = await sectorBondsTask;
            _assetTypeRef       = await assetTypeRefTask;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Dashboard data could not be loaded.");
            ErrorMessage = string.Format(localizationService.Translate("Error_LoadingData"), ex.Message);
        }
        finally
        {
            IsLoading = false;
        }
    }

    // The geography and sector donuts are secondary: their failure must not block the rest of the dashboard, so it
    // leaves them empty instead of failing the whole load — but it is logged, an empty donut alone says nothing.
    private async Task<IReadOnlyList<DistributionDto>> LoadDistributionAsync(
        Func<string, CancellationToken, Task<IReadOnlyList<DistributionDto>>> load, string kind, string assetClass, CancellationToken ct)
    {
        try
        {
            return await load(assetClass, ct);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogWarning(ex, "{Kind} distribution of {AssetClass} could not be loaded; its donut stays empty.", kind, assetClass);
            return [];
        }
    }

    public string GetPanelTitle(PanelState panel)
    {
        if (panel.Level == 0)
            return localizationService.Translate($"Panel_{panel.Type}");

        var segments = Enumerable.Range(0, panel.Level)
            .Select(i => (Index: i, Key: panel.Selected(i)!))
            .Select(s => (panel.Type, s.Index) switch
            {
                (PanelType.Risk, _)       => localizationService.Translate($"Risk_{s.Key}"),
                (PanelType.AssetClass, 1) => panelDistributionService.TranslateAssetType(_assetTypeRef, s.Key),
                _                         => localizationService.Translate(s.Key)
            });

        return string.Join(" › ", segments);
    }

    public void DrillDown(PanelState panel, string name)
    {
        if (IsLeafLevel(panel)) return;
        ClearGeographySelection();
        panel.DrillDown(name);
    }

    public void GoBack(PanelState panel)
    {
        ClearGeographySelection();
        panel.GoBack();
    }

    public void SelectZone(string zone)
    {
        SelectedZone   = zone;
        SelectedSector = null;
    }

    public void SelectSector(string sector)
    {
        SelectedSector = sector;
        SelectedZone   = null;
    }

    public void ClearGeographySelection()
    {
        SelectedZone   = null;
        SelectedSector = null;
    }

    public bool IsLeafLevel(PanelState panel)
    {
        if (panel.Type == PanelType.AssetClass
            && EtfStocksGroupByInformation
            && panel.Selected(1) == AssetTypeNames.EtfStocks)
            return panel.Level >= 3;

        return panel.Type == PanelType.Risk ? panel.Level >= 1 : panel.Level >= 2;
    }

    public decimal GetDistributionTotal(PanelState panel) => GetDistribution(panel).Sum(i => i.CurrentTotal);

    public string GetZonesTitle(PanelState panel) =>
        string.Format(localizationService.Translate("Geo_ZonesTitle"), GetPanelTitle(panel));

    public string GetSectorsTitle(PanelState panel) =>
        string.Format(localizationService.Translate("Geo_SectorsTitle"), GetPanelTitle(panel));

    // Title of the asset list shown once a zone or a sector has been picked.
    public string GetSelectionTitle(PanelState panel) =>
        string.Format(localizationService.Translate("Panel_SelectionTitle"), GetPanelTitle(panel), SelectedZone ?? SelectedSector);

    public IReadOnlyList<DistributionItem> GetDistribution(PanelState panel) =>
        panelDistributionService.GetDistribution(ActiveAssets(), _assetTypeRef, panel, EtfStocksGroupByInformation);

    public IReadOnlyList<AssetDto> GetAssetsForPanel(PanelState panel) =>
        panelDistributionService.GetLeafAssets(ActiveAssets(), panel, EtfStocksGroupByInformation);

    // --- Geography and sectors (distributions computed by the Api, assets filtered here for the drill-down) ---

    public IReadOnlyList<DistributionItem> GetGeographyForClass(string assetClass) =>
        DistributionCalculator.FromApi(assetClass == AssetClassNames.Stocks ? _geoStocks : _geoBonds);

    // Computed by the Api (SectorService) rather than here, so the dashboard and the MAUI analysis workflow
    // share a single rule. The sector name stays the key (used by the drill-down), only its label is translated.
    public IReadOnlyList<DistributionItem> GetSectorForClass(string assetClass) =>
        DistributionCalculator.FromApi(assetClass == AssetClassNames.Stocks ? _sectorStocks : _sectorBonds, localizationService.Translate);

    public IReadOnlyList<AssetDto> GetAssetsForZone(string assetClass, string zone) =>
        GeographySectorFilter.AssetsForZone(ActiveAssets(), _assetTypeRef, assetClass, zone);

    public IReadOnlyDictionary<int, decimal> GetZoneCoefficients(string assetClass, string zone) =>
        GeographySectorFilter.ZoneCoefficients(ActiveAssets(), _assetTypeRef, assetClass, zone);

    public IReadOnlyList<AssetDto> GetAssetsForSector(string assetClass, string sector) =>
        GeographySectorFilter.AssetsForSector(ActiveAssets(), _assetTypeRef, assetClass, sector);

    private IEnumerable<AssetDto> ActiveAssets() =>
        _assets.Where(a => a.CurrentTotal is > 0);
}

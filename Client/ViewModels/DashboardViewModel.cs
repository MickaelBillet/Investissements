using InvestissementsDashboard.Client.Model;
using InvestissementsDashboard.Client.Services;
using InvestissementsDashboard.Shared.Models;

namespace InvestissementsDashboard.Client.ViewModels;

public class DashboardViewModel(IPortfolioService portfolioService)
{
    private IReadOnlyList<AssetDto> _assets = [];

    public SnapshotDto?  LastSnapshot  { get; private set; }
    public bool          IsLoading     { get; private set; } = true;
    public string?       ErrorMessage  { get; private set; }
    public int           AssetCount    => _assets.Count;

    public PanelState AssetClassPanel  { get; } = new(PanelType.AssetClass);
    public PanelState SupportTypePanel { get; } = new(PanelType.SupportType);
    public PanelState RiskPanel        { get; } = new(PanelType.Risk);

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

    public IReadOnlyList<DistributionItem> GetDistribution(PanelState panel)
    {
        var valid = _assets.Where(a => a.CurrentTotal is > 0).ToList();

        var filtered = panel.Level >= 1
            ? valid.Where(BuildFirstLevelFilter(panel))
            : (IEnumerable<AssetDto>)valid;

        Func<AssetDto, string> groupKey = (panel.Type, panel.Level) switch
        {
            (PanelType.AssetClass,  0) => a => a.AssetClass,
            (PanelType.AssetClass,  1) => a => a.AssetType,
            (PanelType.SupportType, 0) => a => a.SupportType,
            (PanelType.SupportType, 1) => a => a.Support,
            (PanelType.Risk,        0) => a => a.Risk.ToString(),
            _                          => a => a.Name
        };

        return ComputeDistribution(filtered, groupKey);
    }

    public IReadOnlyList<AssetDto> GetAssetsForPanel(PanelState panel)
    {
        if (!panel.IsAtLeafLevel) return [];

        var firstLevel = BuildFirstLevelFilter(panel);

        return panel.Type switch
        {
            PanelType.AssetClass  => [.. _assets.Where(a => firstLevel(a) && a.AssetType == panel.Selected(1))],
            PanelType.SupportType => [.. _assets.Where(a => firstLevel(a) && a.Support   == panel.Selected(1))],
            PanelType.Risk        => [.. _assets.Where(firstLevel)],
            _                     => []
        };
    }

    private static Func<AssetDto, bool> BuildFirstLevelFilter(PanelState panel) =>
        panel.Type switch
        {
            PanelType.AssetClass  => a => a.AssetClass  == panel.Selected(0),
            PanelType.SupportType => a => a.SupportType == panel.Selected(0),
            PanelType.Risk        => a => a.Risk.ToString() == panel.Selected(0),
            _                     => _ => true
        };

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

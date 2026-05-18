using InvestissementsDashboard.Shared.Models;

namespace InvestissementsDashboard.Api.Services;

internal sealed class PortfolioMetricsService(IAssetsService assetsService, ISnapshotService snapshotService)
    : IPortfolioMetricsService
{
    public async Task<PortfolioMetricsDto> GetMetricsAsync(CancellationToken ct = default)
    {
        var assetsTask   = assetsService.GetAllAsync(ct);
        var snapshotTask = snapshotService.GetLastAsync(ct);
        await Task.WhenAll(assetsTask, snapshotTask);

        var assets   = await assetsTask;
        var snapshot = await snapshotTask;

        return new PortfolioMetricsDto(
            RoiOnTotalPurchases : ComputeRoiOnTotalPurchases(snapshot),
            RoiOnCapitalEngaged : ComputeRoiOnCapitalEngaged(snapshot),
            AverageRisk         : ComputeAverageRisk(assets));
    }

    public async Task<IReadOnlyList<PerformancePointDto>> GetIndexedHistoryAsync(CancellationToken ct = default)
    {
        var history = await snapshotService.GetHistoryAsync(ct);

        var complete = history
            .Where(s => s.PortfolioTotal > 0
                     && s.TotalPurchases  > 0
                     && s.LifeStrategy60.HasValue
                     && s.MsciWorld.HasValue)
            .OrderBy(s => s.Date)
            .ToList();

        if (complete.Count == 0) return [];

        var t0          = complete[0];
        var t0RoiFactor = RoiFactor(t0);

        return [.. complete.Select(s => new PerformancePointDto(
            s.Date,
            Portfolio     : RoiFactor(s) / t0RoiFactor * 100m,
            LifeStrategy60: s.LifeStrategy60!.Value / t0.LifeStrategy60!.Value * 100m,
            MsciWorld     : s.MsciWorld!.Value      / t0.MsciWorld!.Value      * 100m))];
    }

    // ROI factor = (TotalCurrent + TotalReturns) / TotalPurchases
    private static decimal RoiFactor(SnapshotDto s) =>
        (s.PortfolioTotal + s.TotalReturns) / s.TotalPurchases;

    // ROI (Total des achats) = TotalReturns / TotalPurchases × 100
    private static decimal? ComputeRoiOnTotalPurchases(SnapshotDto? snapshot)
    {
        if (snapshot is null || snapshot.TotalPurchases <= 0m) return null;
        return snapshot.TotalReturns / snapshot.TotalPurchases * 100m;
    }

    // ROI (Capital Engagé) = TotalReturns / PortfolioTotal × 100
    private static decimal? ComputeRoiOnCapitalEngaged(SnapshotDto? snapshot)
    {
        if (snapshot is null || snapshot.PortfolioTotal <= 0m) return null;
        return snapshot.TotalReturns / snapshot.PortfolioTotal * 100m;
    }

    private static decimal? ComputeAverageRisk(IReadOnlyList<AssetDto> assets)
    {
        var active     = assets.Where(a => a.CurrentTotal is > 0).ToList();
        var totalValue = active.Sum(a => a.CurrentTotal ?? 0m);
        if (totalValue == 0m) return null;
        return Math.Round(active.Sum(a => (decimal)a.Risk * (a.CurrentTotal ?? 0m)) / totalValue, 2);
    }
}

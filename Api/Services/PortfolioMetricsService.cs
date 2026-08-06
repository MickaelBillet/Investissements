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
            RoiOnCapitalEngaged : ComputeRoiOnCapitalEngaged(snapshot),
            AverageRisk         : ComputeAverageRisk(assets));
    }

    public async Task<IReadOnlyList<PerformancePointDto>> GetIndexedHistoryAsync(CancellationToken ct = default)
    {
        var history = await snapshotService.GetHistoryAsync(ct);

        var complete = history
            .Where(s => s.NetCapital > 0
                     && s.LifeStrategy.HasValue
                     && s.MsciWorld.HasValue)
            .OrderBy(s => s.Date)
            .ToList();

        if (complete.Count == 0) return [];

        var t0 = complete[0];
        var points = new List<PerformancePointDto>(complete.Count)
        {
            new(t0.Date, 100m, 100m, 100m)
        };

        var roicIndex     = 100m;
        var previous      = t0;
        var previousValue = t0.NetCapital + t0.TotalReturns;

        foreach (var s in complete.Skip(1))
        {
            var value       = s.NetCapital + s.TotalReturns;
            var cashFlow    = s.NetCapital - previous.NetCapital;
            var dailyReturn = previousValue == 0m ? 0m : (value - previousValue - cashFlow) / previousValue;

            roicIndex *= 1 + dailyReturn;

            points.Add(new PerformancePointDto(
                s.Date,
                ROIC          : roicIndex,
                LifeStrategy  : s.LifeStrategy!.Value / t0.LifeStrategy!.Value * 100m,
                MsciWorld     : s.MsciWorld!.Value      / t0.MsciWorld!.Value      * 100m));

            previous      = s;
            previousValue = value;
        }

        return points;
    }

    // ROIC (Capital Engagé) = TotalReturns / NetCapital × 100
    private static decimal? ComputeRoiOnCapitalEngaged(SnapshotDto? snapshot)
    {
        if (snapshot is null || snapshot.NetCapital <= 0m) return null;
        return snapshot.TotalReturns / snapshot.NetCapital * 100m;
    }

    private static decimal? ComputeAverageRisk(IReadOnlyList<AssetDto> assets)
    {
        var active     = assets.Where(a => a.CurrentTotal is > 0).ToList();
        var totalValue = active.Sum(a => a.CurrentTotal ?? 0m);
        if (totalValue == 0m) return null;
        return Math.Round(active.Sum(a => (decimal)a.Risk * (a.CurrentTotal ?? 0m)) / totalValue, 2);
    }
}

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

    private static decimal? ComputeRoiOnTotalPurchases(SnapshotDto? snapshot)
    {
        if (snapshot?.TotalPurchases is not > 0m) return null;
        return (snapshot.TotalReturns ?? 0m) / snapshot.TotalPurchases!.Value * 100m;
    }

    private static decimal? ComputeRoiOnCapitalEngaged(SnapshotDto? snapshot)
    {
        if (snapshot?.PortfolioTotal is not > 0m) return null;
        return (snapshot.TotalReturns ?? 0m) / snapshot.PortfolioTotal * 100m;
    }

    private static decimal? ComputeAverageRisk(IReadOnlyList<AssetDto> assets)
    {
        var active     = assets.Where(a => a.CurrentTotal is > 0).ToList();
        var totalValue = active.Sum(a => a.CurrentTotal ?? 0m);
        if (totalValue == 0m) return null;
        return Math.Round(active.Sum(a => (decimal)a.Risk * (a.CurrentTotal ?? 0m)) / totalValue, 1);
    }
}

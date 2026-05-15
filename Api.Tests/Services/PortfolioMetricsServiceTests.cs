using InvestissementsDashboard.Api.Services;
using InvestissementsDashboard.Shared.Models;
using Moq;
using Xunit;

namespace InvestissementsDashboard.Api.Tests.Services;

public class PortfolioMetricsServiceTests
{
    private static readonly DateOnly AnyDate = new(2025, 1, 1);

    private static PortfolioMetricsService CreateService(
        Mock<IAssetsService>  assetsMock,
        Mock<ISnapshotService> snapshotMock)
        => new(assetsMock.Object, snapshotMock.Object);

    private static Mock<IAssetsService> MockAssets(params AssetDto[] assets)
    {
        var mock = new Mock<IAssetsService>();
        mock.Setup(s => s.GetAllAsync(It.IsAny<CancellationToken>())).ReturnsAsync(assets);
        return mock;
    }

    private static Mock<ISnapshotService> MockSnapshot(SnapshotDto? snapshot)
    {
        var mock = new Mock<ISnapshotService>();
        mock.Setup(s => s.GetLastAsync(It.IsAny<CancellationToken>())).ReturnsAsync(snapshot);
        return mock;
    }

    private static AssetDto Asset(int risk, decimal currentTotal) =>
        new(1, "Test", "Stocks", "PEA", "PEA TR", "ETF_Stocks", "", risk,
            null, null, null, currentTotal, null, null, null, 0m);

    // ── RoiOnTotalPurchases = TotalReturns / TotalPurchases ───────────────────

    [Fact]
    public async Task GetMetricsAsync_WhenSnapshotIsNull_BothRoisAreNull()
    {
        var svc = CreateService(MockAssets(), MockSnapshot(null));

        var result = await svc.GetMetricsAsync();

        Assert.Null(result.RoiOnTotalPurchases);
        Assert.Null(result.RoiOnCapitalEngaged);
    }

    [Fact]
    public async Task GetMetricsAsync_RoiOnTotalPurchases_IsTotalReturnsOverTotalPurchases()
    {
        // TotalReturns=2000, TotalPurchases=10000 → 2000/10000 × 100 = 20 %
        var snapshot = new SnapshotDto(AnyDate, 9_000m, null, null, 10_000m, 2_000m);
        var svc      = CreateService(MockAssets(), MockSnapshot(snapshot));

        var result = await svc.GetMetricsAsync();

        Assert.Equal(20m, result.RoiOnTotalPurchases);
    }

    [Fact]
    public async Task GetMetricsAsync_RoiOnTotalPurchases_WhenNoSales_IsZero()
    {
        var snapshot = new SnapshotDto(AnyDate, 10_000m, null, null, 10_000m, 0m);
        var svc      = CreateService(MockAssets(), MockSnapshot(snapshot));

        var result = await svc.GetMetricsAsync();

        Assert.Equal(0m, result.RoiOnTotalPurchases);
    }

    // ── RoiOnCapitalEngaged = TotalReturns / PortfolioTotal ───────────────────

    [Fact]
    public async Task GetMetricsAsync_RoiOnCapitalEngaged_IsTotalReturnsOverPortfolioTotal()
    {
        // TotalReturns=2000, PortfolioTotal=9000 → 2000/9000 × 100 ≈ 22.22 %
        var snapshot = new SnapshotDto(AnyDate, 9_000m, null, null, 10_000m, 2_000m);
        var svc      = CreateService(MockAssets(), MockSnapshot(snapshot));

        var result = await svc.GetMetricsAsync();

        var expected = 2_000m / 9_000m * 100m;
        Assert.Equal(expected, result.RoiOnCapitalEngaged);
    }

    [Fact]
    public async Task GetMetricsAsync_RoiOnCapitalEngaged_WhenPortfolioTotalIsZero_IsNull()
    {
        var snapshot = new SnapshotDto(AnyDate, 0m, null, null, 10_000m, 2_000m);
        var svc      = CreateService(MockAssets(), MockSnapshot(snapshot));

        var result = await svc.GetMetricsAsync();

        Assert.Null(result.RoiOnCapitalEngaged);
    }

    // ── AverageRisk ───────────────────────────────────────────────────────────

    [Fact]
    public async Task GetMetricsAsync_AverageRisk_WhenNoActiveAssets_IsNull()
    {
        var svc = CreateService(MockAssets(), MockSnapshot(null));

        var result = await svc.GetMetricsAsync();

        Assert.Null(result.AverageRisk);
    }

    [Fact]
    public async Task GetMetricsAsync_AverageRisk_IsWeightedByCurrentTotal()
    {
        // 8000 € at risk 4 + 2000 € at risk 0 → (4×8000 + 0×2000) / 10000 = 3.2
        var svc = CreateService(
            MockAssets(Asset(risk: 4, currentTotal: 8_000m), Asset(risk: 0, currentTotal: 2_000m)),
            MockSnapshot(null));

        var result = await svc.GetMetricsAsync();

        Assert.Equal(3.2m, result.AverageRisk);
    }

    [Fact]
    public async Task GetMetricsAsync_AverageRisk_ExcludesAssetsWithZeroCurrentTotal()
    {
        var svc = CreateService(
            MockAssets(Asset(risk: 2, currentTotal: 1_000m), Asset(risk: 4, currentTotal: 0m)),
            MockSnapshot(null));

        var result = await svc.GetMetricsAsync();

        Assert.Equal(2.0m, result.AverageRisk);
    }

    [Fact]
    public async Task GetMetricsAsync_AverageRisk_IsRoundedToOneDecimal()
    {
        // (1×1000 + 3×2000) / 3000 = 7/3 ≈ 2.333... → 2.3
        var svc = CreateService(
            MockAssets(Asset(risk: 1, currentTotal: 1_000m), Asset(risk: 3, currentTotal: 2_000m)),
            MockSnapshot(null));

        var result = await svc.GetMetricsAsync();

        Assert.Equal(2.3m, result.AverageRisk);
    }
}

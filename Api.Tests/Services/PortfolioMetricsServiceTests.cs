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

    private static Mock<ISnapshotService> MockHistory(params SnapshotDto[] snapshots)
    {
        var mock = new Mock<ISnapshotService>();
        mock.Setup(s => s.GetHistoryAsync(It.IsAny<CancellationToken>())).ReturnsAsync(snapshots);
        return mock;
    }

    private static SnapshotDto Snap(DateOnly date, decimal portfolio, decimal lifeStrategy, decimal msciWorld,
        decimal totalPurchases, decimal totalReturns = 0m)
        => new(date, portfolio, lifeStrategy, msciWorld, totalPurchases, totalReturns);

    private static AssetDto Asset(int risk, decimal currentTotal) =>
        new(1, "Test", "Stocks", "PEA", "PEA TR", "ETF_Stocks", "", "", "", risk,
            null, null, null, currentTotal, null, null, null, 0m);

    // ── RoiOnCapitalEngaged = TotalReturns / PortfolioTotal × 100 ─────────────

    [Fact]
    public async Task GetMetricsAsync_WhenSnapshotIsNull_RoiOnCapitalEngagedIsNull()
    {
        var svc = CreateService(MockAssets(), MockSnapshot(null));

        var result = await svc.GetMetricsAsync();

        Assert.Null(result.RoiOnCapitalEngaged);
    }

    [Fact]
    public async Task GetMetricsAsync_RoiOnCapitalEngaged_IsTotalReturnsOverPortfolioTotal()
    {
        // TotalReturns=667, PortfolioTotal=54890 → 667/54890 × 100 ≈ 1.21 %
        var snapshot = new SnapshotDto(AnyDate, 54_890m, null, null, 71_674m, 667m);
        var svc      = CreateService(MockAssets(), MockSnapshot(snapshot));

        var result = await svc.GetMetricsAsync();

        Assert.Equal(667m / 54_890m * 100m, result.RoiOnCapitalEngaged);
    }

    [Fact]
    public async Task GetMetricsAsync_RoiOnCapitalEngaged_WhenPortfolioTotalIsZero_IsNull()
    {
        var snapshot = new SnapshotDto(AnyDate, 0m, null, null, 71_674m, 667m);
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
    public async Task GetMetricsAsync_AverageRisk_IsRoundedToTwoDecimals()
    {
        // (1×1000 + 3×2000) / 3000 = 7/3 ≈ 2.333... → 2.33
        var svc = CreateService(
            MockAssets(Asset(risk: 1, currentTotal: 1_000m), Asset(risk: 3, currentTotal: 2_000m)),
            MockSnapshot(null));

        var result = await svc.GetMetricsAsync();

        Assert.Equal(2.33m, result.AverageRisk);
    }

    // ── GetIndexedHistoryAsync ────────────────────────────────────────────────

    [Fact]
    public async Task GetIndexedHistoryAsync_WhenNoCompleteSnapshots_ReturnsEmpty()
    {
        // LifeStrategy est null → snapshot incomplet, ignoré
        var svc = CreateService(MockAssets(),
            MockHistory(new SnapshotDto(AnyDate, 10_000m, null, 100m, 10_000m, 0m)));

        var result = await svc.GetIndexedHistoryAsync();

        Assert.Empty(result);
    }

    [Fact]
    public async Task GetIndexedHistoryAsync_FirstEntry_IsAlways100ForAllThreeSeries()
    {
        var svc = CreateService(MockAssets(),
            MockHistory(Snap(AnyDate, 10_000m, 100m, 200m, 10_000m)));

        var result = await svc.GetIndexedHistoryAsync();

        Assert.Single(result);
        Assert.Equal(100m, result[0].ROIC);
        Assert.Equal(100m, result[0].LifeStrategy);
        Assert.Equal(100m, result[0].MsciWorld);
    }

    [Fact]
    public async Task GetIndexedHistoryAsync_ReferenceSeries_IndexedRelativeToT0()
    {
        var d0 = new DateOnly(2025, 1, 1);
        var d1 = new DateOnly(2025, 1, 2);
        var svc = CreateService(MockAssets(),
            MockHistory(
                Snap(d0, 10_000m, 100m, 200m, 10_000m),
                Snap(d1, 11_000m, 110m, 180m, 10_000m)));

        var result = await svc.GetIndexedHistoryAsync();

        Assert.Equal(110m, result[1].LifeStrategy);
        Assert.Equal(90m,  result[1].MsciWorld);
    }
}

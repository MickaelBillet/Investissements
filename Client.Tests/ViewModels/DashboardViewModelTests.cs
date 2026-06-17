using InvestissementsDashboard.Client.Model;
using InvestissementsDashboard.Client.Services;
using InvestissementsDashboard.Client.Tests.Helpers;
using InvestissementsDashboard.Client.ViewModels;
using InvestissementsDashboard.Shared.Models;
using Moq;

namespace InvestissementsDashboard.Client.Tests.ViewModels;

public class DashboardViewModelTests
{
    private static readonly Mock<ILocalizationService> _locMock = CreateLocMock();

    private static Mock<ILocalizationService> CreateLocMock()
    {
        var mock = new Mock<ILocalizationService>();
        mock.Setup(l => l.Translate(It.IsAny<string>())).Returns((string k) => k);
        return mock;
    }

    private static DashboardViewModel CreateVm(Mock<IPortfolioService> mock) =>
        new(mock.Object, _locMock.Object);

    private static Mock<IPortfolioService> MockWithAssets(params AssetDto[] assets)
    {
        var mock = new Mock<IPortfolioService>();
        mock.Setup(s => s.GetAssetsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(assets);
        mock.Setup(s => s.GetLastSnapshotAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync((SnapshotDto?)null);
        mock.Setup(s => s.GetMetricsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync((PortfolioMetricsDto?)null);
        mock.Setup(s => s.GetSnapshotHistoryAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyList<SnapshotDto>)[]);
        mock.Setup(s => s.GetGeographyDistributionAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyList<DistributionDto>)[]);
        return mock;
    }

    private static Mock<IPortfolioService> MockWithMetrics(PortfolioMetricsDto metrics)
    {
        var mock = new Mock<IPortfolioService>();
        mock.Setup(s => s.GetAssetsAsync(It.IsAny<CancellationToken>())).ReturnsAsync([]);
        mock.Setup(s => s.GetLastSnapshotAsync(It.IsAny<CancellationToken>())).ReturnsAsync((SnapshotDto?)null);
        mock.Setup(s => s.GetMetricsAsync(It.IsAny<CancellationToken>())).ReturnsAsync(metrics);
        mock.Setup(s => s.GetSnapshotHistoryAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyList<SnapshotDto>)[]);
        mock.Setup(s => s.GetGeographyDistributionAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyList<DistributionDto>)[]);
        return mock;
    }

    private static Mock<IPortfolioService> MockWithHistory(params SnapshotDto[] history)
    {
        var mock = new Mock<IPortfolioService>();
        mock.Setup(s => s.GetAssetsAsync(It.IsAny<CancellationToken>())).ReturnsAsync([]);
        mock.Setup(s => s.GetLastSnapshotAsync(It.IsAny<CancellationToken>())).ReturnsAsync((SnapshotDto?)null);
        mock.Setup(s => s.GetMetricsAsync(It.IsAny<CancellationToken>())).ReturnsAsync((PortfolioMetricsDto?)null);
        mock.Setup(s => s.GetSnapshotHistoryAsync(It.IsAny<CancellationToken>())).ReturnsAsync(history);
        mock.Setup(s => s.GetGeographyDistributionAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyList<DistributionDto>)[]);
        return mock;
    }

    // ── InitializeAsync ───────────────────────────────────────────────────────

    [Fact]
    public async Task InitializeAsync_WhenServiceReturnsData_PopulatesAssetsAndSetsLoadingFalse()
    {
        var mock = MockWithAssets(TestData.Asset());
        var vm   = CreateVm(mock);

        await vm.InitializeAsync();

        Assert.Equal(1, vm.AssetCount);
        Assert.False(vm.IsLoading);
        Assert.Null(vm.ErrorMessage);
    }

    [Fact]
    public async Task AssetCount_ExcludesAssetsWithZeroCurrentTotal()
    {
        var mock = MockWithAssets(
            TestData.Asset(currentTotal: 1_000m),
            TestData.Asset(currentTotal: 0m),
            TestData.Asset(currentTotal: 500m));
        var vm = CreateVm(mock);

        await vm.InitializeAsync();

        Assert.Equal(2, vm.AssetCount);
    }

    [Fact]
    public async Task InitializeAsync_WhenServiceThrows_SetsErrorMessageAndLoadingFalse()
    {
        var mock = new Mock<IPortfolioService>();
        mock.Setup(s => s.GetAssetsAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new HttpRequestException("Service indisponible"));
        var vm = CreateVm(mock);

        await vm.InitializeAsync();

        Assert.NotNull(vm.ErrorMessage);
        Assert.False(vm.IsLoading);
    }

    [Fact]
    public async Task InitializeAsync_WhenAlreadyLoaded_DoesNotCallServiceAgain()
    {
        var mock = MockWithAssets(TestData.Asset());
        var vm   = CreateVm(mock);

        await vm.InitializeAsync();
        await vm.InitializeAsync();

        mock.Verify(s => s.GetAssetsAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    // ── GetDistribution ───────────────────────────────────────────────────────

    [Fact]
    public async Task GetDistribution_WhenAssetClassLevel0_GroupsByAssetClass()
    {
        var mock = MockWithAssets(
            TestData.Asset(assetClass: "Stocks", currentTotal: 6_000m),
            TestData.Asset(assetClass: "Bonds",  currentTotal: 4_000m));
        var vm = CreateVm(mock);
        await vm.InitializeAsync();

        var distribution = vm.GetDistribution(vm.AssetClassPanel);

        Assert.Equal(2, distribution.Count);
        Assert.Contains(distribution, d => d.Name == "Stocks");
        Assert.Contains(distribution, d => d.Name == "Bonds");
    }

    [Fact]
    public async Task GetDistribution_WhenAssetClassLevel1AfterDrillDown_FiltersByClassAndGroupsByType()
    {
        var mock = MockWithAssets(
            TestData.Asset(assetClass: "Stocks", assetType: "ETF_Stocks", currentTotal: 6_000m),
            TestData.Asset(assetClass: "Stocks", assetType: "Stock",       currentTotal: 4_000m),
            TestData.Asset(assetClass: "Bonds",  assetType: "ETF_Bunds",   currentTotal: 2_000m));
        var vm = CreateVm(mock);
        await vm.InitializeAsync();
        vm.AssetClassPanel.DrillDown("Stocks");

        var distribution = vm.GetDistribution(vm.AssetClassPanel);

        Assert.Equal(2, distribution.Count);
        Assert.All(distribution, d => Assert.NotEqual("ETF_Bunds", d.Name));
    }

    [Fact]
    public async Task GetDistribution_WhenRiskLevel0_GroupsByRisk()
    {
        var mock = MockWithAssets(
            TestData.Asset(risk: 1, currentTotal: 3_000m),
            TestData.Asset(risk: 3, currentTotal: 7_000m));
        var vm = CreateVm(mock);
        await vm.InitializeAsync();

        var distribution = vm.GetDistribution(vm.RiskPanel);

        Assert.Equal(2, distribution.Count);
        Assert.Contains(distribution, d => d.Name == "1");
        Assert.Contains(distribution, d => d.Name == "3");
    }

    [Fact]
    public async Task GetDistribution_WeightsSumTo100()
    {
        var mock = MockWithAssets(
            TestData.Asset(name: "A", currentTotal: 3_000m),
            TestData.Asset(name: "B", currentTotal: 7_000m));
        var vm = CreateVm(mock);
        await vm.InitializeAsync();

        var total = vm.GetDistribution(vm.AssetClassPanel).Sum(d => d.Weight);

        Assert.Equal(100m, total, precision: 2);
    }

    // ── GetAssetsForPanel ─────────────────────────────────────────────────────

    [Fact]
    public async Task GetAssetsForPanel_WhenNotAtLeafLevel_ReturnsEmpty()
    {
        var mock = MockWithAssets(TestData.Asset());
        var vm   = CreateVm(mock);
        await vm.InitializeAsync();

        var result = vm.GetAssetsForPanel(vm.AssetClassPanel); // Level 0

        Assert.Empty(result);
    }

    [Fact]
    public async Task GetAssetsForPanel_WhenAssetClassLeaf_ReturnsOnlyMatchingAssets()
    {
        var mock = MockWithAssets(
            TestData.Asset(name: "A", assetClass: "Stocks", assetType: "ETF_Stocks", currentTotal: 1_000m),
            TestData.Asset(name: "B", assetClass: "Bonds",  assetType: "ETF_Bunds",  currentTotal: 500m));
        var vm = CreateVm(mock);
        await vm.InitializeAsync();
        vm.AssetClassPanel.DrillDown("Stocks");
        vm.AssetClassPanel.DrillDown("ETF_Stocks");

        var result = vm.GetAssetsForPanel(vm.AssetClassPanel);

        Assert.Single(result);
        Assert.Equal("A", result[0].Name);
    }

    [Fact]
    public async Task GetAssetsForPanel_WhenRiskLeaf_ReturnsOnlyAssetsWithSelectedRisk()
    {
        var mock = MockWithAssets(
            TestData.Asset(name: "Risk3", risk: 3, currentTotal: 1_000m),
            TestData.Asset(name: "Risk1", risk: 1, currentTotal: 500m));
        var vm = CreateVm(mock);
        await vm.InitializeAsync();
        vm.RiskPanel.DrillDown("3");

        var result = vm.GetAssetsForPanel(vm.RiskPanel);

        Assert.Single(result);
        Assert.Equal("Risk3", result[0].Name);
    }

    [Fact]
    public async Task GetAssetsForPanel_WhenSupportTypeLeaf_ReturnsOnlyMatchingAssets()
    {
        var mock = MockWithAssets(
            TestData.Asset(name: "A", supportType: "PEA", support: "PEA TR",         currentTotal: 2_000m),
            TestData.Asset(name: "B", supportType: "CTO", support: "Trade Republic",  currentTotal: 1_000m));
        var vm = CreateVm(mock);
        await vm.InitializeAsync();
        vm.SupportTypePanel.DrillDown("PEA");
        vm.SupportTypePanel.DrillDown("PEA TR");

        var result = vm.GetAssetsForPanel(vm.SupportTypePanel);

        Assert.Single(result);
        Assert.Equal("A", result[0].Name);
    }

    // ── PortfolioRoi + AverageRisk (proxy vers API metrics) ──────────────────

    [Fact]
    public async Task PortfolioRoiOnCapitalEngaged_WhenMetricsIsNull_IsNull()
    {
        var vm = CreateVm(MockWithAssets());
        await vm.InitializeAsync();

        Assert.Null(vm.PortfolioRoiOnCapitalEngaged);
    }

    [Fact]
    public async Task PortfolioRoiOnCapitalEngaged_ExposesValueFromMetrics()
    {
        var vm = CreateVm(MockWithMetrics(new PortfolioMetricsDto(9.09m, 2.5m)));
        await vm.InitializeAsync();

        Assert.Equal(9.09m, vm.PortfolioRoiOnCapitalEngaged);
    }

    [Fact]
    public async Task AverageRisk_WhenMetricsIsNull_IsNull()
    {
        var vm = CreateVm(MockWithAssets());
        await vm.InitializeAsync();

        Assert.Null(vm.AverageRisk);
    }

    [Fact]
    public async Task AverageRisk_ExposesValueFromMetrics()
    {
        var vm = CreateVm(MockWithMetrics(new PortfolioMetricsDto(null, 2.3m)));
        await vm.InitializeAsync();

        Assert.Equal(2.3m, vm.AverageRisk);
    }

    // ── IsLeafLevel ───────────────────────────────────────────────────────────

    [Fact]
    public void IsLeafLevel_WhenAssetClassLevel2AndToggleOff_ReturnsTrue()
    {
        var vm = CreateVm(new Mock<IPortfolioService>());
        vm.AssetClassPanel.DrillDown("Stocks");
        vm.AssetClassPanel.DrillDown("ETF_Stocks");

        Assert.True(vm.IsLeafLevel(vm.AssetClassPanel));
    }

    [Fact]
    public void IsLeafLevel_WhenAssetClassLevel2EtfStocksAndToggleOn_ReturnsFalse()
    {
        var vm = CreateVm(new Mock<IPortfolioService>());
        vm.AssetClassPanel.DrillDown("Stocks");
        vm.AssetClassPanel.DrillDown("ETF_Stocks");
        vm.EtfStocksGroupByInformation = true;

        Assert.False(vm.IsLeafLevel(vm.AssetClassPanel));
    }

    [Fact]
    public void IsLeafLevel_WhenAssetClassLevel3AndToggleOn_ReturnsTrue()
    {
        var vm = CreateVm(new Mock<IPortfolioService>());
        vm.AssetClassPanel.DrillDown("Stocks");
        vm.AssetClassPanel.DrillDown("ETF_Stocks");
        vm.AssetClassPanel.DrillDown("World");
        vm.EtfStocksGroupByInformation = true;

        Assert.True(vm.IsLeafLevel(vm.AssetClassPanel));
    }

    [Fact]
    public void IsLeafLevel_WhenOtherAssetTypeLevel2AndToggleOn_ReturnsTrue()
    {
        var vm = CreateVm(new Mock<IPortfolioService>());
        vm.AssetClassPanel.DrillDown("Stocks");
        vm.AssetClassPanel.DrillDown("Stock");
        vm.EtfStocksGroupByInformation = true;

        // Toggle only applies to ETF_Stocks — other types remain leaf at level 2
        Assert.True(vm.IsLeafLevel(vm.AssetClassPanel));
    }

    [Fact]
    public void IsLeafLevel_WhenRiskLevel1_ReturnsTrue()
    {
        var vm = CreateVm(new Mock<IPortfolioService>());
        vm.RiskPanel.DrillDown("3");

        Assert.True(vm.IsLeafLevel(vm.RiskPanel));
    }

    // ── GetDistribution ETF grouping ──────────────────────────────────────────

    [Fact]
    public async Task GetDistribution_WhenEtfStocksLevel2AndToggleOn_GroupsByInformation()
    {
        var mock = MockWithAssets(
            TestData.Asset(name: "MSCI World",    assetClass: "Stocks", assetType: "ETF_Stocks", information: "World",  currentTotal: 6_000m),
            TestData.Asset(name: "Stoxx 600",     assetClass: "Stocks", assetType: "ETF_Stocks", information: "Europe", currentTotal: 3_000m),
            TestData.Asset(name: "Hydrogen",      assetClass: "Stocks", assetType: "ETF_Stocks", information: "Europe", currentTotal: 1_000m));
        var vm = CreateVm(mock);
        await vm.InitializeAsync();
        vm.AssetClassPanel.DrillDown("Stocks");
        vm.AssetClassPanel.DrillDown("ETF_Stocks");
        vm.EtfStocksGroupByInformation = true;

        var distribution = vm.GetDistribution(vm.AssetClassPanel);

        Assert.Equal(2, distribution.Count);
        Assert.Contains(distribution, d => d.Name == "World");
        Assert.Contains(distribution, d => d.Name == "Europe");
    }

    [Fact]
    public async Task GetDistribution_WhenEtfStocksLevel2AndToggleOff_GroupsByName()
    {
        var mock = MockWithAssets(
            TestData.Asset(name: "MSCI World", assetClass: "Stocks", assetType: "ETF_Stocks", information: "World",  currentTotal: 6_000m),
            TestData.Asset(name: "Stoxx 600",  assetClass: "Stocks", assetType: "ETF_Stocks", information: "Europe", currentTotal: 3_000m));
        var vm = CreateVm(mock);
        await vm.InitializeAsync();
        vm.AssetClassPanel.DrillDown("Stocks");
        vm.AssetClassPanel.DrillDown("ETF_Stocks");
        vm.EtfStocksGroupByInformation = false;

        var distribution = vm.GetDistribution(vm.AssetClassPanel);

        Assert.Equal(2, distribution.Count);
        Assert.Contains(distribution, d => d.Name == "MSCI World");
        Assert.Contains(distribution, d => d.Name == "Stoxx 600");
    }

    [Fact]
    public async Task GetDistribution_WhenEtfStocksLevel3AndToggleOn_GroupsByName()
    {
        var mock = MockWithAssets(
            TestData.Asset(name: "MSCI World",  assetClass: "Stocks", assetType: "ETF_Stocks", information: "World",  currentTotal: 6_000m),
            TestData.Asset(name: "World Small", assetClass: "Stocks", assetType: "ETF_Stocks", information: "World",  currentTotal: 2_000m),
            TestData.Asset(name: "Stoxx 600",   assetClass: "Stocks", assetType: "ETF_Stocks", information: "Europe", currentTotal: 3_000m));
        var vm = CreateVm(mock);
        await vm.InitializeAsync();
        vm.AssetClassPanel.DrillDown("Stocks");
        vm.AssetClassPanel.DrillDown("ETF_Stocks");
        vm.AssetClassPanel.DrillDown("World");
        vm.EtfStocksGroupByInformation = true;

        var distribution = vm.GetDistribution(vm.AssetClassPanel);

        Assert.Equal(2, distribution.Count);
        Assert.Contains(distribution, d => d.Name == "MSCI World");
        Assert.Contains(distribution, d => d.Name == "World Small");
        Assert.DoesNotContain(distribution, d => d.Name == "Stoxx 600");
    }

    // ── GetSectorForClass ─────────────────────────────────────────────────────

    [Fact]
    public async Task GetSectorForClass_GroupsBySectorForGivenAssetClass()
    {
        var mock = MockWithAssets(
            TestData.Asset(assetClass: "Stocks", sector: "Technology", currentTotal: 6_000m),
            TestData.Asset(assetClass: "Stocks", sector: "Finance",    currentTotal: 4_000m),
            TestData.Asset(assetClass: "Bonds",  sector: "Finance",    currentTotal: 2_000m));
        var vm = CreateVm(mock);
        await vm.InitializeAsync();

        var result = vm.GetSectorForClass("Stocks");

        Assert.Equal(2, result.Count);
        Assert.Contains(result, d => d.Name == "Technology");
        Assert.Contains(result, d => d.Name == "Finance");
    }

    [Fact]
    public async Task GetSectorForClass_ExcludesEtfBunds()
    {
        var mock = MockWithAssets(
            TestData.Asset(assetClass: "Bonds", assetType: "MarketBonds",  sector: "Finance", currentTotal: 4_000m),
            TestData.Asset(assetClass: "Bonds", assetType: "ETF_Bunds",    sector: "Finance", currentTotal: 2_000m));
        var vm = CreateVm(mock);
        await vm.InitializeAsync();

        var result = vm.GetSectorForClass("Bonds");

        Assert.Single(result);
        Assert.Equal(4_000m, result[0].CurrentTotal);
    }

    [Fact]
    public async Task GetSectorForClass_ExcludesAssetsWithEmptySector()
    {
        var mock = MockWithAssets(
            TestData.Asset(assetClass: "Stocks", sector: "Technology", currentTotal: 6_000m),
            TestData.Asset(assetClass: "Stocks", sector: "",            currentTotal: 2_000m));
        var vm = CreateVm(mock);
        await vm.InitializeAsync();

        var result = vm.GetSectorForClass("Stocks");

        Assert.Single(result);
        Assert.Equal("Technology", result[0].Name);
    }

    // ── GetAssetsForSector ────────────────────────────────────────────────────

    [Fact]
    public async Task GetAssetsForSector_FiltersAssetsByClassAndSector()
    {
        var mock = MockWithAssets(
            TestData.Asset(name: "A", assetClass: "Stocks", sector: "Technology", currentTotal: 6_000m),
            TestData.Asset(name: "B", assetClass: "Stocks", sector: "Finance",    currentTotal: 4_000m),
            TestData.Asset(name: "C", assetClass: "Bonds",  sector: "Technology", currentTotal: 2_000m));
        var vm = CreateVm(mock);
        await vm.InitializeAsync();

        var result = vm.GetAssetsForSector("Stocks", "Technology");

        Assert.Single(result);
        Assert.Equal("A", result[0].Name);
    }

    [Fact]
    public async Task GetAssetsForSector_IsSortedByCurrentTotalDescending()
    {
        var mock = MockWithAssets(
            TestData.Asset(name: "Small", assetClass: "Stocks", sector: "Technology", currentTotal: 1_000m),
            TestData.Asset(name: "Large", assetClass: "Stocks", sector: "Technology", currentTotal: 5_000m));
        var vm = CreateVm(mock);
        await vm.InitializeAsync();

        var result = vm.GetAssetsForSector("Stocks", "Technology");

        Assert.Equal("Large", result[0].Name);
        Assert.Equal("Small", result[1].Name);
    }

    [Fact]
    public async Task GetAssetsForPanel_WhenEtfGroupedAtLevel3_FiltersAssetsByInformationGroup()
    {
        var mock = MockWithAssets(
            TestData.Asset(name: "MSCI World",  assetClass: "Stocks", assetType: "ETF_Stocks", information: "World",  currentTotal: 6_000m),
            TestData.Asset(name: "World Small", assetClass: "Stocks", assetType: "ETF_Stocks", information: "World",  currentTotal: 2_000m),
            TestData.Asset(name: "Stoxx 600",   assetClass: "Stocks", assetType: "ETF_Stocks", information: "Europe", currentTotal: 3_000m));
        var vm = CreateVm(mock);
        await vm.InitializeAsync();
        vm.AssetClassPanel.DrillDown("Stocks");
        vm.AssetClassPanel.DrillDown("ETF_Stocks");
        vm.AssetClassPanel.DrillDown("World");
        vm.EtfStocksGroupByInformation = true;

        var result = vm.GetAssetsForPanel(vm.AssetClassPanel);

        Assert.Equal(2, result.Count);
        Assert.All(result, a => Assert.Equal("World", a.Information));
        Assert.DoesNotContain(result, a => a.Name == "Stoxx 600");
    }

    // ── DailyROICapitalEngagedVariation / WeeklyROITotalPurchasesVariation ──────

    [Fact]
    public async Task DailyROICapitalEngagedVariation_WhenTwoEntries_ReturnsRelativeChange()
    {
        // ROI_CE hier = 1_000 / 50_000 * 100 = 2 %
        // ROI_CE today = 1_100 / 52_000 * 100 ≈ 2,1154 %
        // variation relative = (2,1154 - 2) / |2| * 100 ≈ 5,77 %
        var mock = MockWithHistory(
            TestData.Snapshot(date: new DateOnly(2026, 5, 19), portfolio: 50_000m, totalReturns: 1_000m),
            TestData.Snapshot(date: new DateOnly(2026, 5, 20), portfolio: 52_000m, totalReturns: 1_100m));
        var vm = CreateVm(mock);
        await vm.InitializeAsync();

        var roiRef  = 1_000m / 50_000m * 100m;
        var roiLast = 1_100m / 52_000m * 100m;
        var expected = (roiLast - roiRef) / Math.Abs(roiRef) * 100m;
        Assert.Equal(expected, vm.DailyROICapitalEngagedVariation);
    }

    [Fact]
    public async Task DailyROICapitalEngagedVariation_WhenReferenceROIIsZero_ReturnsNull()
    {
        // ROI_ref = 0 → division par zéro → null
        var mock = MockWithHistory(
            TestData.Snapshot(date: new DateOnly(2026, 5, 19), portfolio: 50_000m, totalReturns: 0m),
            TestData.Snapshot(date: new DateOnly(2026, 5, 20), portfolio: 52_000m, totalReturns: 1_000m));
        var vm = CreateVm(mock);
        await vm.InitializeAsync();

        Assert.Null(vm.DailyROICapitalEngagedVariation);
    }

    [Fact]
    public async Task WeeklyROICapitalEngagedVariation_WhenEntryExactlySevenDaysBack_ReturnsRelativeChange()
    {
        // ref (13 mai) : ROI = 1_000 / 50_000 * 100 = 2 %
        // today (20 mai) : ROI = 1_040 / 52_000 * 100 = 2 %
        // variation relative = (2 - 2) / 2 * 100 = 0 %
        var mock = MockWithHistory(
            TestData.Snapshot(date: new DateOnly(2026, 5, 13), portfolio: 50_000m, totalReturns: 1_000m),
            TestData.Snapshot(date: new DateOnly(2026, 5, 17), portfolio: 51_000m, totalReturns: 1_020m),
            TestData.Snapshot(date: new DateOnly(2026, 5, 20), portfolio: 52_000m, totalReturns: 1_040m));
        var vm = CreateVm(mock);
        await vm.InitializeAsync();

        Assert.Equal(0m, vm.WeeklyROICapitalEngagedVariation);
    }

    // ── DailyVariationPercent / WeeklyVariationPercent ────────────────────────

    [Fact]
    public async Task DailyVariationPercent_WhenHistoryEmpty_ReturnsNull()
    {
        var mock = MockWithHistory();
        var vm   = CreateVm(mock);
        await vm.InitializeAsync();

        Assert.Null(vm.DailyVariationPercent);
    }

    [Fact]
    public async Task DailyVariationPercent_WhenOnlyOneEntry_ReturnsNull()
    {
        var mock = MockWithHistory(TestData.Snapshot(date: new DateOnly(2026, 5, 19), portfolio: 50_000m));
        var vm   = CreateVm(mock);
        await vm.InitializeAsync();

        Assert.Null(vm.DailyVariationPercent);
    }

    [Fact]
    public async Task DailyVariationPercent_WhenTwoEntries_ReturnsCorrectPercent()
    {
        var mock = MockWithHistory(
            TestData.Snapshot(date: new DateOnly(2026, 5, 19), portfolio: 50_000m),
            TestData.Snapshot(date: new DateOnly(2026, 5, 20), portfolio: 51_000m));
        var vm = CreateVm(mock);
        await vm.InitializeAsync();

        Assert.Equal(2m, vm.DailyVariationPercent);
    }

    [Fact]
    public async Task DailyVariationPercent_WhenPreviousTotalIsZero_ReturnsNull()
    {
        var mock = MockWithHistory(
            TestData.Snapshot(date: new DateOnly(2026, 5, 19), portfolio: 0m),
            TestData.Snapshot(date: new DateOnly(2026, 5, 20), portfolio: 51_000m));
        var vm = CreateVm(mock);
        await vm.InitializeAsync();

        Assert.Null(vm.DailyVariationPercent);
    }

    [Fact]
    public async Task WeeklyVariationPercent_WhenEntryExactlySevenDaysBack_ReturnsCorrectPercent()
    {
        var mock = MockWithHistory(
            TestData.Snapshot(date: new DateOnly(2026, 5, 13), portfolio: 50_000m),
            TestData.Snapshot(date: new DateOnly(2026, 5, 17), portfolio: 51_000m),
            TestData.Snapshot(date: new DateOnly(2026, 5, 20), portfolio: 52_000m));
        var vm = CreateVm(mock);
        await vm.InitializeAsync();

        // ref = 2026-05-13 (≤ 2026-05-20 − 7j = 2026-05-13), last = 52_000
        Assert.Equal(4m, vm.WeeklyVariationPercent);
    }

    [Fact]
    public async Task WeeklyVariationPercent_WhenNoEntrySevenDaysBack_ReturnsNull()
    {
        var mock = MockWithHistory(
            TestData.Snapshot(date: new DateOnly(2026, 5, 15), portfolio: 50_000m),
            TestData.Snapshot(date: new DateOnly(2026, 5, 20), portfolio: 52_000m));
        var vm = CreateVm(mock);
        await vm.InitializeAsync();

        // aucune entrée ≤ 2026-05-20 − 7j = 2026-05-13
        Assert.Null(vm.WeeklyVariationPercent);
    }

    // ── MonthlyVariationPercent / YearlyVariationPercent ──────────────────────

    [Fact]
    public async Task MonthlyVariationPercent_WhenEntryThirtyDaysBack_ReturnsCorrectPercent()
    {
        var mock = MockWithHistory(
            TestData.Snapshot(date: new DateOnly(2026, 4, 20), portfolio: 50_000m),
            TestData.Snapshot(date: new DateOnly(2026, 5,  5), portfolio: 51_000m),
            TestData.Snapshot(date: new DateOnly(2026, 5, 20), portfolio: 55_000m));
        var vm = CreateVm(mock);
        await vm.InitializeAsync();

        // ref = 2026-04-20 (≤ 2026-05-20 − 30j = 2026-04-20), last = 55_000 → +10 %
        Assert.Equal(10m, vm.MonthlyVariationPercent);
    }

    [Fact]
    public async Task MonthlyVariationPercent_WhenNoEntryThirtyDaysBack_ReturnsNull()
    {
        var mock = MockWithHistory(
            TestData.Snapshot(date: new DateOnly(2026, 5,  5), portfolio: 50_000m),
            TestData.Snapshot(date: new DateOnly(2026, 5, 20), portfolio: 55_000m));
        var vm = CreateVm(mock);
        await vm.InitializeAsync();

        Assert.Null(vm.MonthlyVariationPercent);
    }

    [Fact]
    public async Task YearlyVariationPercent_WhenEntryOneYearBack_ReturnsCorrectPercent()
    {
        var mock = MockWithHistory(
            TestData.Snapshot(date: new DateOnly(2025, 5, 20), portfolio: 40_000m),
            TestData.Snapshot(date: new DateOnly(2026, 5, 20), portfolio: 52_000m));
        var vm = CreateVm(mock);
        await vm.InitializeAsync();

        // ref = 2025-05-20 (≤ 2026-05-20 − 365j), last = 52_000 → +30 %
        Assert.Equal(30m, vm.YearlyVariationPercent);
    }

    // ── YtdVariationPercent (référence = 1er snapshot de l'année courante) ─────

    [Fact]
    public async Task YtdVariationPercent_UsesFirstSnapshotOfCurrentYear()
    {
        var mock = MockWithHistory(
            TestData.Snapshot(date: new DateOnly(2025, 12, 31), portfolio: 30_000m),
            TestData.Snapshot(date: new DateOnly(2026,  1,  2), portfolio: 40_000m),
            TestData.Snapshot(date: new DateOnly(2026,  3, 15), portfolio: 45_000m),
            TestData.Snapshot(date: new DateOnly(2026,  5, 20), portfolio: 48_000m));
        var vm = CreateVm(mock);
        await vm.InitializeAsync();

        // ref = 2026-01-02 (1er snapshot de 2026), last = 48_000 → +20 %
        Assert.Equal(20m, vm.YtdVariationPercent);
    }

    [Fact]
    public async Task YtdVariationPercent_WhenSingleSnapshotInYear_ReturnsZero()
    {
        var mock = MockWithHistory(
            TestData.Snapshot(date: new DateOnly(2025, 12, 31), portfolio: 30_000m),
            TestData.Snapshot(date: new DateOnly(2026,  5, 20), portfolio: 48_000m));
        var vm = CreateVm(mock);
        await vm.InitializeAsync();

        // un seul snapshot en 2026 → référence == dernier → 0 %
        Assert.Equal(0m, vm.YtdVariationPercent);
    }

    [Fact]
    public async Task YtdROICapitalEngagedVariation_UsesFirstSnapshotOfCurrentYear()
    {
        var mock = MockWithHistory(
            TestData.Snapshot(date: new DateOnly(2026, 1,  2), portfolio: 50_000m, totalReturns: 1_000m),
            TestData.Snapshot(date: new DateOnly(2026, 5, 20), portfolio: 50_000m, totalReturns: 1_500m));
        var vm = CreateVm(mock);
        await vm.InitializeAsync();

        // ROI_ref = 1_000 / 50_000 * 100 = 2 % ; ROI_last = 1_500 / 50_000 * 100 = 3 %
        // variation relative = (3 - 2) / 2 * 100 = 50 %
        Assert.Equal(50m, vm.YtdROICapitalEngagedVariation);
    }
}

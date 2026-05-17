using InvestissementsDashboard.Client.Model;
using InvestissementsDashboard.Client.Services;
using InvestissementsDashboard.Client.Tests.Helpers;
using InvestissementsDashboard.Client.ViewModels;
using InvestissementsDashboard.Shared.Models;
using Moq;

namespace InvestissementsDashboard.Client.Tests.ViewModels;

public class DashboardViewModelTests
{
    private static DashboardViewModel CreateVm(Mock<IPortfolioService> mock) => new(mock.Object);

    private static Mock<IPortfolioService> MockWithAssets(params AssetDto[] assets)
    {
        var mock = new Mock<IPortfolioService>();
        mock.Setup(s => s.GetAssetsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(assets);
        mock.Setup(s => s.GetLastSnapshotAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync((SnapshotDto?)null);
        mock.Setup(s => s.GetMetricsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync((PortfolioMetricsDto?)null);
        return mock;
    }

    private static Mock<IPortfolioService> MockWithMetrics(PortfolioMetricsDto metrics)
    {
        var mock = new Mock<IPortfolioService>();
        mock.Setup(s => s.GetAssetsAsync(It.IsAny<CancellationToken>())).ReturnsAsync([]);
        mock.Setup(s => s.GetLastSnapshotAsync(It.IsAny<CancellationToken>())).ReturnsAsync((SnapshotDto?)null);
        mock.Setup(s => s.GetMetricsAsync(It.IsAny<CancellationToken>())).ReturnsAsync(metrics);
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
    public async Task PortfolioRoi_WhenMetricsIsNull_BothRoisAreNull()
    {
        var vm = CreateVm(MockWithAssets());
        await vm.InitializeAsync();

        Assert.Null(vm.PortfolioRoiOnTotalPurchases);
        Assert.Null(vm.PortfolioRoiOnCapitalEngaged);
    }

    [Fact]
    public async Task PortfolioRoiOnTotalPurchases_ExposesValueFromMetrics()
    {
        var vm = CreateVm(MockWithMetrics(new PortfolioMetricsDto(10m, 9.09m, 2.5m)));
        await vm.InitializeAsync();

        Assert.Equal(10m, vm.PortfolioRoiOnTotalPurchases);
    }

    [Fact]
    public async Task PortfolioRoiOnCapitalEngaged_ExposesValueFromMetrics()
    {
        var vm = CreateVm(MockWithMetrics(new PortfolioMetricsDto(10m, 9.09m, 2.5m)));
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
        var vm = CreateVm(MockWithMetrics(new PortfolioMetricsDto(null, null, 2.3m)));
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
}

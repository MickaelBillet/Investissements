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
}

using InvestissementsDashboard.Api.Services;
using InvestissementsDashboard.Shared.Models;
using Moq;
using Xunit;

namespace InvestissementsDashboard.Api.Tests.Services;

public class SectorServiceTests
{
    private static SectorService CreateService(Mock<IAssetsService> mock) => new(mock.Object);

    private static readonly AssetTypeReferenceDto[] DefaultAssetTypeReference =
    [
        new(1, "Stock",       "Action",             true),
        new(2, "ETF_Stocks",  "ETF Actions",        true),
        new(3, "MarketBonds", "Obligations cotées", true),
        new(4, "ETF_Bunds",   "ETF Obligations",    false),
        new(5, "OPCVM",       "OPCVM",              false),
    ];

    private static Mock<IAssetsService> MockAssets(params AssetDto[] assets)
    {
        var mock = new Mock<IAssetsService>();
        mock.Setup(s => s.GetAllAsync(It.IsAny<CancellationToken>())).ReturnsAsync(assets);
        mock.Setup(s => s.GetAssetTypeReferenceAsync(It.IsAny<CancellationToken>())).ReturnsAsync(DefaultAssetTypeReference);
        return mock;
    }

    private static AssetDto Asset(string assetClass, string assetType, string sector, decimal? currentTotal) =>
        new(1, "Test", assetClass, "PEA", "PEA TR", assetType, sector, "", "", 3,
            null, null, null, currentTotal, null, null, null, 0m);

    [Fact]
    public async Task GetDistributionAsync_SumsCurrentTotalBySector()
    {
        var mock = MockAssets(
            Asset("Stocks", "Stock",      "Technology", 6_000m),
            Asset("Stocks", "ETF_Stocks", "Technology", 2_000m),
            Asset("Stocks", "Stock",      "Finance",    2_000m));

        var result = await CreateService(mock).GetDistributionAsync("Stocks");

        Assert.Equal(2, result.Count);
        var tech = result.Single(d => d.Name == "Technology");
        Assert.Equal(8_000m, tech.CurrentTotal);
        Assert.Equal(80m, tech.WeightInPortfolio);
        var finance = result.Single(d => d.Name == "Finance");
        Assert.Equal(2_000m, finance.CurrentTotal);
        Assert.Equal(20m, finance.WeightInPortfolio);
    }

    [Fact]
    public async Task GetDistributionAsync_OtherAssetClass_IsExcluded()
    {
        var mock = MockAssets(
            Asset("Stocks", "Stock",       "Technology", 1_000m),
            Asset("Bonds",  "MarketBonds", "Sovereign",  5_000m));

        var result = await CreateService(mock).GetDistributionAsync("Stocks");

        Assert.Single(result);
        Assert.Equal("Technology", result[0].Name);
    }

    [Fact]
    public async Task GetDistributionAsync_TypeNotGeoSectorEligible_IsExcluded()
    {
        var mock = MockAssets(
            Asset("Bonds", "MarketBonds", "Sovereign", 4_000m),
            Asset("Bonds", "ETF_Bunds",   "Sovereign", 2_000m));

        var result = await CreateService(mock).GetDistributionAsync("Bonds");

        Assert.Single(result);
        Assert.Equal(4_000m, result[0].CurrentTotal);
    }

    [Fact]
    public async Task GetDistributionAsync_TypeAbsentFromReference_IsExcluded()
    {
        var mock = MockAssets(
            Asset("Stocks", "Stock",   "Technology", 1_000m),
            Asset("Stocks", "Unknown", "Technology", 9_000m));

        var result = await CreateService(mock).GetDistributionAsync("Stocks");

        Assert.Single(result);
        Assert.Equal(1_000m, result[0].CurrentTotal);
    }

    [Theory]
    [InlineData(null)]
    [InlineData(0)]
    [InlineData(-500)]
    public async Task GetDistributionAsync_CurrentTotalNotPositive_IsExcluded(int? currentTotal)
    {
        var mock = MockAssets(
            Asset("Stocks", "Stock", "Technology", 1_000m),
            Asset("Stocks", "Stock", "Finance",    currentTotal));

        var result = await CreateService(mock).GetDistributionAsync("Stocks");

        Assert.Single(result);
        Assert.Equal("Technology", result[0].Name);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task GetDistributionAsync_EmptySector_IsExcluded(string sector)
    {
        var mock = MockAssets(
            Asset("Stocks", "Stock", "Technology", 1_000m),
            Asset("Stocks", "Stock", sector,       3_000m));

        var result = await CreateService(mock).GetDistributionAsync("Stocks");

        Assert.Single(result);
        Assert.Equal("Technology", result[0].Name);
        Assert.Equal(100m, result[0].WeightInPortfolio);
    }

    [Fact]
    public async Task GetDistributionAsync_IsSortedByCurrentTotalDescending()
    {
        var mock = MockAssets(
            Asset("Stocks", "Stock", "Small",  1_000m),
            Asset("Stocks", "Stock", "Large",  5_000m),
            Asset("Stocks", "Stock", "Medium", 3_000m));

        var result = await CreateService(mock).GetDistributionAsync("Stocks");

        Assert.Equal(["Large", "Medium", "Small"], result.Select(d => d.Name));
    }

    [Fact]
    public async Task GetDistributionAsync_WeightIsRoundedToTwoDecimals()
    {
        var mock = MockAssets(
            Asset("Stocks", "Stock", "A", 1m),
            Asset("Stocks", "Stock", "B", 1m),
            Asset("Stocks", "Stock", "C", 1m));

        var result = await CreateService(mock).GetDistributionAsync("Stocks");

        Assert.All(result, d => Assert.Equal(33.33m, d.WeightInPortfolio));
    }

    [Fact]
    public async Task GetDistributionAsync_NoMatchingAsset_ReturnsEmpty()
    {
        var mock = MockAssets(Asset("Bonds", "MarketBonds", "Sovereign", 1_000m));

        var result = await CreateService(mock).GetDistributionAsync("Stocks");

        Assert.Empty(result);
    }
}

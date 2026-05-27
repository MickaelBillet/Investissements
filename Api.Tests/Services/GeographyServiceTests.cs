using InvestissementsDashboard.Api.Services;
using InvestissementsDashboard.Shared.Models;
using Moq;
using Xunit;

namespace InvestissementsDashboard.Api.Tests.Services;

public class GeographyServiceTests
{
    private static GeographyService CreateService(Mock<IAssetsService> mock) => new(mock.Object);

    private static Mock<IAssetsService> MockAssets(params AssetDto[] assets)
    {
        var mock = new Mock<IAssetsService>();
        mock.Setup(s => s.GetAllAsync(It.IsAny<CancellationToken>())).ReturnsAsync(assets);
        return mock;
    }

    private static AssetDto Asset(string assetClass, string assetType, string geography, decimal currentTotal) =>
        new(1, "Test", assetClass, "PEA", "PEA TR", assetType, "", "", geography, 3,
            null, null, null, currentTotal, null, null, null, 0m);

    // ── ParseGeography ────────────────────────────────────────────────────────

    [Fact]
    public void ParseGeography_StandardFormat_ParsesCorrectly()
    {
        var result = GeographyService.ParseGeography("USA : 41% - Europe : 24% - UK : 5%").ToList();

        Assert.Equal(3, result.Count);
        Assert.Equal(("USA",    0.41m), result[0]);
        Assert.Equal(("Europe", 0.24m), result[1]);
        Assert.Equal(("UK",     0.05m), result[2]);
    }

    [Fact]
    public void ParseGeography_SingleZone_ParsesCorrectly()
    {
        var result = GeographyService.ParseGeography("Europe : 100%").ToList();

        Assert.Single(result);
        Assert.Equal(("Europe", 1.00m), result[0]);
    }

    [Fact]
    public void ParseGeography_EmptyString_ReturnsEmpty()
    {
        Assert.Empty(GeographyService.ParseGeography(""));
        Assert.Empty(GeographyService.ParseGeography("   "));
    }

    [Fact]
    public void ParseGeography_ZoneWithSpaces_ParsesCorrectly()
    {
        var result = GeographyService.ParseGeography("South America : 75% - Mexique : 25%").ToList();

        Assert.Equal(2, result.Count);
        Assert.Equal("South America", result[0].Zone);
        Assert.Equal(0.75m, result[0].Pct);
    }

    // ── GetDistributionAsync ──────────────────────────────────────────────────

    [Fact]
    public async Task GetDistributionAsync_WeightsZonesByCurrentTotal()
    {
        // Asset 1 (Stocks/ETF_Stocks): Europe 24%, USA 41%, currentTotal=1000
        // Asset 2 (Stocks/ETF_Stocks): Europe 100%, currentTotal=500
        // Europe = 0.24×1000 + 1.00×500 = 740
        var svc = CreateService(MockAssets(
            Asset("Stocks", "ETF_Stocks", "USA : 41% - Europe : 24%", 1000m),
            Asset("Stocks", "ETF_Stocks", "Europe : 100%", 500m)));

        var result = await svc.GetDistributionAsync("Stocks");

        var europe = result.FirstOrDefault(d => d.Name == "Europe");
        Assert.NotNull(europe);
        Assert.Equal(740m, europe.CurrentTotal);
    }

    [Fact]
    public async Task GetDistributionAsync_FiltersToRequestedAssetClass()
    {
        var svc = CreateService(MockAssets(
            Asset("Stocks", "ETF_Stocks", "Europe : 100%", 1000m),
            Asset("Bonds",  "ETF_Bunds",  "Europe : 100%", 2000m)));

        var result = await svc.GetDistributionAsync("Stocks");

        var europe = result.First(d => d.Name == "Europe");
        Assert.Equal(1000m, europe.CurrentTotal);
    }

    [Fact]
    public async Task GetDistributionAsync_ExcludesIneligibleAssetTypes()
    {
        var svc = CreateService(MockAssets(
            Asset("Stocks", "ETF_Stocks", "Europe : 100%", 1000m),
            Asset("Stocks", "OPCVM",      "Europe : 100%", 2000m)));

        var result = await svc.GetDistributionAsync("Stocks");

        var europe = result.First(d => d.Name == "Europe");
        Assert.Equal(1000m, europe.CurrentTotal);
    }

    [Fact]
    public async Task GetDistributionAsync_ExcludesEtfBunds()
    {
        var svc = CreateService(MockAssets(
            Asset("Bonds", "MarketBonds", "Europe : 100%", 1000m),
            Asset("Bonds", "ETF_Bunds",   "USA : 100%",    2000m)));

        var result = await svc.GetDistributionAsync("Bonds");

        Assert.Single(result);
        Assert.Equal("Europe", result[0].Name);
        Assert.Equal(1000m, result[0].CurrentTotal);
    }

    [Fact]
    public async Task GetDistributionAsync_ExcludesAssetsWithEmptyGeography()
    {
        var svc = CreateService(MockAssets(
            Asset("Stocks", "ETF_Stocks", "Europe : 100%", 1000m),
            Asset("Stocks", "ETF_Stocks", "",              2000m)));

        var result = await svc.GetDistributionAsync("Stocks");

        Assert.Single(result);
        Assert.Equal(1000m, result[0].CurrentTotal);
    }

    [Fact]
    public async Task GetDistributionAsync_WeightInPortfolioSumsTo100()
    {
        var svc = CreateService(MockAssets(
            Asset("Stocks", "ETF_Stocks", "Europe : 60% - USA : 40%", 1000m)));

        var result = await svc.GetDistributionAsync("Stocks");

        var totalWeight = result.Sum(d => d.WeightInPortfolio);
        Assert.Equal(100m, totalWeight);
    }

    [Fact]
    public async Task GetDistributionAsync_SortedByCurrentTotalDescending()
    {
        var svc = CreateService(MockAssets(
            Asset("Stocks", "ETF_Stocks", "USA : 70% - Europe : 30%", 1000m)));

        var result = await svc.GetDistributionAsync("Stocks");

        Assert.True(result[0].CurrentTotal >= result[1].CurrentTotal);
    }
}

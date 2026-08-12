using InvestissementsDashboard.Api.Services;
using InvestissementsDashboard.GoogleSheets;
using InvestissementsDashboard.Shared.Models;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace InvestissementsDashboard.Api.Tests.Services;

public class AssetsServiceTests
{
    private static readonly IReadOnlyList<object> Header = ["Id", "Name"];

    private static AssetsService CreateService(Mock<IGoogleSheetsClient> mock)
        => new(mock.Object, new MemoryCache(new MemoryCacheOptions()), NullLogger<AssetsService>.Instance);

    private static Mock<IGoogleSheetsClient> MockAssetSheet(params IReadOnlyList<object>[] rows)
    {
        var mock = new Mock<IGoogleSheetsClient>();
        IReadOnlyList<IReadOnlyList<object>> sheet = [Header, .. rows];
        mock.Setup(s => s.GetRangeAsync("Asset", It.IsAny<CancellationToken>())).ReturnsAsync(sheet);
        return mock;
    }

    private static IReadOnlyList<object> AssetRow(
        int id, string name, string assetClass, string supportType, string support, string assetType,
        string sector, string information, string geography, int risk,
        object totalPurchases, object totalSales, object dividends, object currentTotal)
        => [id, name, assetClass, supportType, support, assetType, sector, information, geography, risk,
            totalPurchases, totalSales, dividends, currentTotal];

    [Fact]
    public async Task GetAllAsync_WhenSheetHasAssets_ReturnsComputedDtos()
    {
        var mock = MockAssetSheet(
            AssetRow(1, "MSCI World", "Stocks", "PEA", "PEA TR", "ETF_Stocks", "", "", "", 4, 5000d, 0d, 0d, 6000d),
            AssetRow(2, "Livret A", "Cash", "Booklet", "Livret A", "Savings", "", "", "", 0, 3000d, 0d, 0d, 3000d));

        var result = await CreateService(mock).GetAllAsync();

        Assert.Equal(2, result.Count);
        Assert.Equal("MSCI World", result[0].Name);
        Assert.Equal(20m, result[0].Roi);
        Assert.Equal(66.67m, result[0].WeightInPortfolio);
        Assert.Equal("Livret A", result[1].Name);
        Assert.Equal(33.33m, result[1].WeightInPortfolio);
    }

    [Fact]
    public async Task GetAllAsync_SkipsNotDefinedRows()
    {
        var mock = MockAssetSheet(
            AssetRow(1, "Not Defined", "Stocks", "PEA", "PEA TR", "ETF_Stocks", "", "", "", 4, 5000d, 0d, 0d, 6000d),
            AssetRow(2, "Livret A", "Cash", "Booklet", "Livret A", "Savings", "", "", "", 0, 3000d, 0d, 0d, 3000d));

        var result = await CreateService(mock).GetAllAsync();

        Assert.Single(result);
        Assert.Equal("Livret A", result[0].Name);
    }

    [Fact]
    public async Task GetAllAsync_WhenSheetIsEmpty_ReturnsEmptyList()
    {
        var mock = MockAssetSheet();

        var result = await CreateService(mock).GetAllAsync();

        Assert.Empty(result);
    }

    [Fact]
    public async Task GetAllAsync_CalledTwiceWithinTtl_ReadsSheetOnce()
    {
        var mock = MockAssetSheet(
            AssetRow(1, "MSCI World", "Stocks", "PEA", "PEA TR", "ETF_Stocks", "", "", "", 4, 5000d, 0d, 0d, 6000d));

        var service = CreateService(mock);
        await service.GetAllAsync();
        await service.GetAllAsync();

        mock.Verify(s => s.GetRangeAsync("Asset", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetDistributionByDimensionAsync_WhenValidDimension_GroupsByAssetClass()
    {
        var mock = MockAssetSheet(
            AssetRow(1, "A", "Stocks", "PEA", "PEA TR", "ETF_Stocks", "", "", "", 4, 5000d, 0d, 0d, 6000d),
            AssetRow(2, "B", "Stocks", "PEA", "PEA TR", "ETF_Stocks", "", "", "", 4, 2000d, 0d, 0d, 2000d),
            AssetRow(3, "C", "Cash", "Booklet", "Livret A", "Savings", "", "", "", 0, 3000d, 0d, 0d, 3000d));

        var result = await CreateService(mock).GetDistributionByDimensionAsync("assetClass");

        Assert.Equal(2, result.Count);
        Assert.Contains(result, d => d.Name == "Stocks" && d.CurrentTotal == 8000m);
        Assert.Contains(result, d => d.Name == "Cash" && d.CurrentTotal == 3000m);
    }

    [Fact]
    public async Task GetDistributionByDimensionAsync_WhenUnknownDimension_ThrowsArgumentException()
    {
        var mock = MockAssetSheet();

        await Assert.ThrowsAsync<ArgumentException>(() =>
            CreateService(mock).GetDistributionByDimensionAsync("unknown"));

        mock.Verify(s => s.GetRangeAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Theory]
    [InlineData("assetClass",  "AC1")]
    [InlineData("assetType",   "AT1")]
    [InlineData("supportType", "ST1")]
    [InlineData("support",     "S1")]
    public async Task GetDistributionByDimensionAsync_MapsAllDimensionsToCorrectColumn(string dimension, string expectedGroup)
    {
        var mock = MockAssetSheet(
            AssetRow(1, "X", "AC1", "ST1", "S1", "AT1", "", "", "", 0, 100d, 0d, 0d, 100d));

        var result = await CreateService(mock).GetDistributionByDimensionAsync(dimension);

        Assert.Single(result);
        Assert.Equal(expectedGroup, result[0].Name);
    }

    [Fact]
    public async Task GetEtfStocksByInformationAsync_GroupsByInformation()
    {
        var mock = MockAssetSheet(
            AssetRow(1, "A", "Stocks", "PEA", "PEA TR", "ETF_Stocks", "", "World", "", 4, 5000d, 0d, 0d, 6000d),
            AssetRow(2, "B", "Stocks", "PEA", "PEA TR", "ETF_Stocks", "", "Europe", "", 4, 2000d, 0d, 0d, 2400d),
            AssetRow(3, "C", "Cash", "Booklet", "Livret A", "Savings", "", "", "", 0, 1000d, 0d, 0d, 1000d));

        var result = await CreateService(mock).GetEtfStocksByInformationAsync();

        Assert.Equal(2, result.Count);
        Assert.Contains(result, g => g.Name == "World" && g.CurrentTotal == 6000m);
        Assert.Contains(result, g => g.Name == "Europe" && g.CurrentTotal == 2400m);
    }

    [Fact]
    public async Task GetEtfStocksByInformationAsync_WhenNoEtfStocks_ReturnsEmptyList()
    {
        var mock = MockAssetSheet(
            AssetRow(1, "C", "Cash", "Booklet", "Livret A", "Savings", "", "", "", 0, 1000d, 0d, 0d, 1000d));

        var result = await CreateService(mock).GetEtfStocksByInformationAsync();

        Assert.Empty(result);
    }

    [Fact]
    public async Task GetByAssetTypeAndInformationAsync_FiltersMatchingAssets()
    {
        var mock = MockAssetSheet(
            AssetRow(1, "MSCI World ETF", "Stocks", "PEA", "PEA TR", "ETF_Stocks", "", "World", "", 4, 5000d, 0d, 0d, 6000d),
            AssetRow(2, "Other", "Stocks", "PEA", "PEA TR", "ETF_Stocks", "", "Europe", "", 4, 2000d, 0d, 0d, 2400d));

        var result = await CreateService(mock).GetByAssetTypeAndInformationAsync("ETF_Stocks", "World");

        Assert.Single(result);
        Assert.Equal("MSCI World ETF", result[0].Name);
    }

    [Fact]
    public async Task GetByAssetTypeAndInformationAsync_WhenNoMatch_ReturnsEmptyList()
    {
        var mock = MockAssetSheet(
            AssetRow(1, "MSCI World ETF", "Stocks", "PEA", "PEA TR", "ETF_Stocks", "", "World", "", 4, 5000d, 0d, 0d, 6000d));

        var result = await CreateService(mock).GetByAssetTypeAndInformationAsync("ETF_Stocks", "Europe");

        Assert.Empty(result);
    }

    // --- AssetType reference ---

    private static Mock<IGoogleSheetsClient> MockAssetTypeSheet(params IReadOnlyList<object>[] rows)
    {
        var mock = new Mock<IGoogleSheetsClient>();
        IReadOnlyList<IReadOnlyList<object>> sheet = [Header, .. rows];
        mock.Setup(s => s.GetRangeAsync("AssetType", It.IsAny<CancellationToken>())).ReturnsAsync(sheet);
        return mock;
    }

    private static IReadOnlyList<object> AssetTypeRow(int id, string name, string assetClass, string labelFr, object geoSectorEligible)
        => [id, name, assetClass, labelFr, geoSectorEligible];

    [Fact]
    public async Task GetAssetTypeReferenceAsync_WhenSheetHasRows_ReturnsMappedDtos()
    {
        var mock = MockAssetTypeSheet(
            AssetTypeRow(1, "Stock", "Stocks", "Action", true),
            AssetTypeRow(2, "Savings", "Cash", "Épargne", false));

        var result = await CreateService(mock).GetAssetTypeReferenceAsync();

        Assert.Equal(2, result.Count);
        Assert.Equal("Action", result[0].LabelFr);
        Assert.True(result[0].GeoSectorEligible);
        Assert.False(result[1].GeoSectorEligible);
    }

    [Fact]
    public async Task GetAssetTypeReferenceAsync_ParsesTextualBooleans()
    {
        var mock = MockAssetTypeSheet(
            AssetTypeRow(1, "Stock", "Stocks", "Action", "TRUE"),
            AssetTypeRow(2, "SCI_SCPI", "RealEstate", "SCPI", "OUI"),
            AssetTypeRow(3, "Savings", "Cash", "Épargne", "FALSE"));

        var result = await CreateService(mock).GetAssetTypeReferenceAsync();

        Assert.True(result[0].GeoSectorEligible);
        Assert.True(result[1].GeoSectorEligible);
        Assert.False(result[2].GeoSectorEligible);
    }

    [Fact]
    public async Task GetAssetTypeReferenceAsync_WhenSheetIsEmpty_ReturnsEmptyList()
    {
        var mock = MockAssetTypeSheet();

        var result = await CreateService(mock).GetAssetTypeReferenceAsync();

        Assert.Empty(result);
    }

    [Fact]
    public async Task GetAssetTypeReferenceAsync_CalledTwiceWithinTtl_ReadsSheetOnce()
    {
        var mock = MockAssetTypeSheet(AssetTypeRow(1, "Stock", "Stocks", "Action", true));

        var service = CreateService(mock);
        await service.GetAssetTypeReferenceAsync();
        await service.GetAssetTypeReferenceAsync();

        mock.Verify(s => s.GetRangeAsync("AssetType", It.IsAny<CancellationToken>()), Times.Once);
    }
}

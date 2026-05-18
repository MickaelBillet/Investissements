using InvestissementsDashboard.Api.Services;
using InvestissementsDashboard.Shared.Models;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace InvestissementsDashboard.Api.Tests.Services;

public class AssetsServiceTests
{
    private static AssetsService CreateService(Mock<IAppsScriptService> mock)
        => new(mock.Object, NullLogger<AssetsService>.Instance);

    [Fact]
    public async Task GetAllAsync_WhenAppsScriptReturnsAssets_ReturnsThem()
    {
        var expected = new[]
        {
            new AssetDto(1, "MSCI World", "Stocks", "PEA", "PEA TR", "ETF_Stocks", "", "", "", 4,
                5000m, 0m, 0m, 6000m, 1000m, 0m, 20m, 60m),
            new AssetDto(2, "Livret A", "Cash", "Booklet", "Livret A", "Savings", "", "", "", 0,
                3000m, 0m, 0m, 3000m, 0m, 0m, 0m, 40m)
        };
        var mock = new Mock<IAppsScriptService>();
        mock.Setup(s => s.CallAsync<IReadOnlyList<AssetDto>>("Asset", "getAll", It.IsAny<CancellationToken>()))
            .ReturnsAsync(expected);

        var result = await CreateService(mock).GetAllAsync();

        Assert.Equal(2, result.Count);
        Assert.Equal("MSCI World", result[0].Name);
        Assert.Equal("Livret A", result[1].Name);
    }

    [Fact]
    public async Task GetAllAsync_WhenAppsScriptReturnsNull_ReturnsEmptyList()
    {
        var mock = new Mock<IAppsScriptService>();
        mock.Setup(s => s.CallAsync<IReadOnlyList<AssetDto>>("Asset", "getAll", It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyList<AssetDto>?)null);

        var result = await CreateService(mock).GetAllAsync();

        Assert.Empty(result);
    }

    [Fact]
    public async Task GetDistributionByDimensionAsync_WhenValidDimension_CallsCorrectService()
    {
        var expected = new[] { new DistributionDto("Stocks", 10000m, 80m, 0) };
        var mock = new Mock<IAppsScriptService>();
        mock.Setup(s => s.CallAsync<IReadOnlyList<DistributionDto>>("AssetClass", "getDistribution", It.IsAny<CancellationToken>()))
            .ReturnsAsync(expected);

        var result = await CreateService(mock).GetDistributionByDimensionAsync("assetClass");

        Assert.Single(result);
        Assert.Equal("Stocks", result[0].Name);
        mock.Verify(s => s.CallAsync<IReadOnlyList<DistributionDto>>("AssetClass", "getDistribution", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetDistributionByDimensionAsync_WhenUnknownDimension_ThrowsArgumentException()
    {
        var mock = new Mock<IAppsScriptService>();

        await Assert.ThrowsAsync<ArgumentException>(() =>
            CreateService(mock).GetDistributionByDimensionAsync("unknown"));

        mock.Verify(s => s.CallAsync<IReadOnlyList<DistributionDto>>(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Theory]
    [InlineData("assetClass",  "AssetClass")]
    [InlineData("assetType",   "AssetType")]
    [InlineData("supportType", "SupportType")]
    [InlineData("support",     "Support")]
    public async Task GetDistributionByDimensionAsync_MapsAllDimensionsToCorrectService(string dimension, string expectedService)
    {
        var mock = new Mock<IAppsScriptService>();
        mock.Setup(s => s.CallAsync<IReadOnlyList<DistributionDto>>(It.IsAny<string>(), "getDistribution", It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        await CreateService(mock).GetDistributionByDimensionAsync(dimension);

        mock.Verify(s => s.CallAsync<IReadOnlyList<DistributionDto>>(expectedService, "getDistribution", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetEtfStocksByInformationAsync_WhenAppsScriptReturnsGroups_ReturnsThem()
    {
        var expected = new[]
        {
            new AggregateDto("World", 5000m, 0m, 0m, 6000m, false, 1000m, 0m, 20m, 60m, 50m),
            new AggregateDto("Europe", 2000m, 0m, 0m, 2400m, false, 400m, 0m, 20m, 40m, 20m)
        };
        var mock = new Mock<IAppsScriptService>();
        mock.Setup(s => s.CallAsync<IReadOnlyList<AggregateDto>>(
                "AssetType", "getEtfStocksByInformation",
                It.IsAny<IReadOnlyDictionary<string, string>?>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(expected);

        var result = await CreateService(mock).GetEtfStocksByInformationAsync();

        Assert.Equal(2, result.Count);
        Assert.Equal("World", result[0].Name);
        Assert.Equal("Europe", result[1].Name);
    }

    [Fact]
    public async Task GetEtfStocksByInformationAsync_WhenAppsScriptReturnsNull_ReturnsEmptyList()
    {
        var mock = new Mock<IAppsScriptService>();
        mock.Setup(s => s.CallAsync<IReadOnlyList<AggregateDto>>(
                "AssetType", "getEtfStocksByInformation",
                It.IsAny<IReadOnlyDictionary<string, string>?>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyList<AggregateDto>?)null);

        var result = await CreateService(mock).GetEtfStocksByInformationAsync();

        Assert.Empty(result);
    }

    [Fact]
    public async Task GetByAssetTypeAndInformationAsync_WhenAppsScriptReturnsAssets_ReturnsThem()
    {
        var expected = new[]
        {
            new AssetDto(1, "MSCI World ETF", "Stocks", "PEA", "PEA TR", "ETF_Stocks", "", "World", "", 4,
                5000m, 0m, 0m, 6000m, 1000m, 0m, 20m, 60m)
        };
        var mock = new Mock<IAppsScriptService>();
        mock.Setup(s => s.CallAsync<IReadOnlyList<AssetDto>>(
                "AssetType", "getByAssetTypeAndInformation",
                It.IsAny<IReadOnlyDictionary<string, string>?>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(expected);

        var result = await CreateService(mock).GetByAssetTypeAndInformationAsync("ETF_Stocks", "World");

        Assert.Single(result);
        Assert.Equal("MSCI World ETF", result[0].Name);
        mock.Verify(s => s.CallAsync<IReadOnlyList<AssetDto>>(
            "AssetType", "getByAssetTypeAndInformation",
            It.Is<IReadOnlyDictionary<string, string>?>(d =>
                d != null && d["assetType"] == "ETF_Stocks" && d["information"] == "World"),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetByAssetTypeAndInformationAsync_WhenAppsScriptReturnsNull_ReturnsEmptyList()
    {
        var mock = new Mock<IAppsScriptService>();
        mock.Setup(s => s.CallAsync<IReadOnlyList<AssetDto>>(
                "AssetType", "getByAssetTypeAndInformation",
                It.IsAny<IReadOnlyDictionary<string, string>?>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyList<AssetDto>?)null);

        var result = await CreateService(mock).GetByAssetTypeAndInformationAsync("ETF_Stocks", "World");

        Assert.Empty(result);
    }
}

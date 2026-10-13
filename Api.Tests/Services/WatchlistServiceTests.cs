using InvestissementsDashboard.Api.Services;
using InvestissementsDashboard.GoogleSheets;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace InvestissementsDashboard.Api.Tests.Services;

public class WatchlistServiceTests
{
    private static readonly IReadOnlyList<object> Header = ["Name", "ISIN"];

    private static WatchlistService CreateService(Mock<IGoogleSheetsClient> mock)
        => new(mock.Object, new MemoryCache(new MemoryCacheOptions()), NullLogger<WatchlistService>.Instance);

    private static Mock<IGoogleSheetsClient> MockWatchlistSheet(params IReadOnlyList<object>[] rows)
    {
        var mock = new Mock<IGoogleSheetsClient>();
        IReadOnlyList<IReadOnlyList<object>> sheet = [Header, .. rows];
        mock.Setup(s => s.GetRangeAsync("Watchlist", It.IsAny<CancellationToken>())).ReturnsAsync(sheet);
        return mock;
    }

    [Fact]
    public async Task GetAllAsync_WhenSheetHasRows_ReturnsMappedItems()
    {
        var mock = MockWatchlistSheet(
            ["Hermes", "FR0000052292"],
            ["Iberdrola", "ES0144580Y14"]);

        var result = await CreateService(mock).GetAllAsync();

        Assert.Equal(2, result.Count);
        Assert.Equal("Hermes", result[0].Name);
        Assert.Equal("FR0000052292", result[0].Isin);
        Assert.Equal("Iberdrola", result[1].Name);
    }

    [Fact]
    public async Task GetAllAsync_WhenRowHasNoName_SkipsRow()
    {
        var mock = MockWatchlistSheet(
            ["", "FR0000052292"],
            ["Euronext", "NL0006294274"]);

        var result = await CreateService(mock).GetAllAsync();

        var item = Assert.Single(result);
        Assert.Equal("Euronext", item.Name);
    }

    [Fact]
    public async Task GetAllAsync_WhenIsinCellIsMissing_ReturnsEmptyIsin()
    {
        var mock = MockWatchlistSheet(["Lifco AB"]);

        var result = await CreateService(mock).GetAllAsync();

        var item = Assert.Single(result);
        Assert.Equal("", item.Isin);
    }

    [Fact]
    public async Task GetAllAsync_WhenCellsHaveSurroundingSpaces_TrimsValues()
    {
        var mock = MockWatchlistSheet([" Euronext ", " NL0006294274 "]);

        var result = await CreateService(mock).GetAllAsync();

        var item = Assert.Single(result);
        Assert.Equal("Euronext", item.Name);
        Assert.Equal("NL0006294274", item.Isin);
    }

    [Fact]
    public async Task GetAllAsync_WhenSheetHasOnlyHeader_ReturnsEmptyList()
    {
        var mock = MockWatchlistSheet();

        var result = await CreateService(mock).GetAllAsync();

        Assert.Empty(result);
    }

    [Fact]
    public async Task GetAllAsync_CalledTwice_ReadsSheetOnce()
    {
        var mock = MockWatchlistSheet(["Hermes", "FR0000052292"]);
        var service = CreateService(mock);

        await service.GetAllAsync();
        await service.GetAllAsync();

        mock.Verify(s => s.GetRangeAsync("Watchlist", It.IsAny<CancellationToken>()), Times.Once);
    }
}

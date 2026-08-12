using InvestissementsDashboard.Api.Services;
using InvestissementsDashboard.GoogleSheets;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace InvestissementsDashboard.Api.Tests.Services;

public class SnapshotServiceTests
{
    private static readonly IReadOnlyList<object> Header = ["Date", "NetCapital"];

    private static SnapshotService CreateService(Mock<IGoogleSheetsClient> mock)
        => new(mock.Object, new MemoryCache(new MemoryCacheOptions()), NullLogger<SnapshotService>.Instance);

    private static Mock<IGoogleSheetsClient> MockSnapshotSheet(params IReadOnlyList<object>[] rows)
    {
        var mock = new Mock<IGoogleSheetsClient>();
        IReadOnlyList<IReadOnlyList<object>> sheet = [Header, .. rows];
        mock.Setup(s => s.GetRangeAsync("Snapshot", It.IsAny<CancellationToken>())).ReturnsAsync(sheet);
        return mock;
    }

    private static IReadOnlyList<object> SnapshotRow(
        object date, object netCapital, object lifeStrategy, object msciWorld,
        object totalPurchases, object totalReturns, object totalSales)
        => [date, netCapital, lifeStrategy, msciWorld, totalPurchases, totalReturns, totalSales];

    [Fact]
    public async Task GetLastAsync_WhenSheetHasRows_ReturnsLastRow()
    {
        var mock = MockSnapshotSheet(
            SnapshotRow("2026-05-01", 70000d, 40.1d, 80.2d, 60000d, 75000d, 900d),
            SnapshotRow("2026-05-02", 72000d, 41d,   81d,   60000d, 76000d, 1000d));

        var result = await CreateService(mock).GetLastAsync();

        Assert.NotNull(result);
        Assert.Equal(new DateOnly(2026, 5, 2), result.Date);
        Assert.Equal(72000m, result.NetCapital);
    }

    [Fact]
    public async Task GetLastAsync_WhenDateIsSheetsSerialNumber_ParsesCorrectly()
    {
        // Google Sheets stores date-looking text as a real date internally, so reads come
        // back as a serial number (days since 1899-12-30) — deserialized as long, not double.
        var expectedDate = new DateOnly(2026, 5, 2);
        var serial = (long)(expectedDate.ToDateTime(TimeOnly.MinValue) - new DateTime(1899, 12, 30)).TotalDays;
        var mock = MockSnapshotSheet(SnapshotRow(serial, 72000d, 41d, 81d, 60000d, 76000d, 1000d));

        var result = await CreateService(mock).GetLastAsync();

        Assert.NotNull(result);
        Assert.Equal(expectedDate, result.Date);
    }

    [Fact]
    public async Task GetLastAsync_WhenSheetIsEmpty_ReturnsNull()
    {
        var mock = MockSnapshotSheet();

        var result = await CreateService(mock).GetLastAsync();

        Assert.Null(result);
    }

    [Fact]
    public async Task GetLastAsync_CalledTwiceWithinTtl_ReadsSheetOnce()
    {
        var mock = MockSnapshotSheet(
            SnapshotRow("2026-05-02", 72000d, 41d, 81d, 60000d, 76000d, 1000d));

        var service = CreateService(mock);
        await service.GetLastAsync();
        await service.GetLastAsync();

        mock.Verify(s => s.GetRangeAsync("Snapshot", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetHistoryAsync_WhenSheetHasRows_ReturnsThemInOrder()
    {
        var mock = MockSnapshotSheet(
            SnapshotRow("2026-05-01", 70000d, 40.1d, 80.2d, 60000d, 75000d, 900d),
            SnapshotRow("2026-05-02", 72000d, 41d,   81d,   60000d, 76000d, 1000d));

        var result = await CreateService(mock).GetHistoryAsync();

        Assert.Equal(2, result.Count);
        Assert.Equal(new DateOnly(2026, 5, 1), result[0].Date);
        Assert.Equal(new DateOnly(2026, 5, 2), result[1].Date);
    }

    [Fact]
    public async Task GetHistoryAsync_WhenSheetIsEmpty_ReturnsEmptyList()
    {
        var mock = MockSnapshotSheet();

        var result = await CreateService(mock).GetHistoryAsync();

        Assert.Empty(result);
    }

    [Fact]
    public async Task GetHistoryAsync_CalledTwiceWithinTtl_ReadsSheetOnce()
    {
        var mock = MockSnapshotSheet(
            SnapshotRow("2026-05-01", 70000d, 40.1d, 80.2d, 60000d, 75000d, 900d));

        var service = CreateService(mock);
        await service.GetHistoryAsync();
        await service.GetHistoryAsync();

        mock.Verify(s => s.GetRangeAsync("Snapshot", It.IsAny<CancellationToken>()), Times.Once);
    }
}

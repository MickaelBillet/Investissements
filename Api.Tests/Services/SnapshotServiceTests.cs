using InvestissementsDashboard.Api.Services;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace InvestissementsDashboard.Api.Tests.Services;

public class SnapshotServiceTests
{
    private static readonly IReadOnlyList<string> HeaderRow = ["Date", "PortfolioTotal", "LifeStrategy60", "MsciWorld", "TotalPurchases", "TotalReturns"];

    private static IReadOnlyList<IReadOnlyList<string>> BuildSheet(params IReadOnlyList<string>[] dataRows)
        => new[] { HeaderRow }.Concat(dataRows).ToList();

    private static SnapshotService CreateService(IReadOnlyList<IReadOnlyList<string>> rows)
    {
        var mock = new Mock<IGoogleSheetsService>();
        mock.Setup(s => s.GetRangeAsync("Snapshot", It.IsAny<CancellationToken>()))
            .ReturnsAsync(rows);
        return new SnapshotService(mock.Object, NullLogger<SnapshotService>.Instance);
    }

    [Fact]
    public async Task GetLastAsync_WhenSheetHasRows_ReturnsLastSnapshot()
    {
        var sheet = BuildSheet(
            ["2026-05-01", "70000.00", "40.10", "80.20", "60000.00", "75000.00"],
            ["2026-05-02", "72000.00", "41.00", "81.00", "60000.00", "76000.00"]
        );
        var service = CreateService(sheet);

        var result = await service.GetLastAsync();

        Assert.NotNull(result);
        Assert.Equal(new DateOnly(2026, 5, 2), result.Date);
        Assert.Equal(72000.00m, result.PortfolioTotal);
        Assert.Equal(41.00m, result.LifeStrategy60);
    }

    [Fact]
    public async Task GetLastAsync_WhenSheetIsEmpty_ReturnsNull()
    {
        var sheet = BuildSheet();
        var service = CreateService(sheet);

        var result = await service.GetLastAsync();

        Assert.Null(result);
    }

    [Fact]
    public async Task GetHistoryAsync_WhenSheetHasRows_ReturnsAllSnapshotsAscending()
    {
        var sheet = BuildSheet(
            ["2026-05-02", "72000.00", "41.00", "81.00", "60000.00", "76000.00"],
            ["2026-05-01", "70000.00", "40.10", "80.20", "60000.00", "75000.00"]
        );
        var service = CreateService(sheet);

        var result = await service.GetHistoryAsync();

        Assert.Equal(2, result.Count);
        Assert.Equal(new DateOnly(2026, 5, 1), result[0].Date);
        Assert.Equal(new DateOnly(2026, 5, 2), result[1].Date);
    }

    [Fact]
    public async Task GetHistoryAsync_WhenValueIsEmpty_SetsNullableFieldToNull()
    {
        var sheet = BuildSheet(
            ["2026-05-01", "70000.00", "", "", "", ""]
        );
        var service = CreateService(sheet);

        var result = await service.GetHistoryAsync();

        Assert.Single(result);
        Assert.Null(result[0].LifeStrategy60);
        Assert.Null(result[0].MsciWorld);
        Assert.Null(result[0].TotalPurchases);
        Assert.Null(result[0].TotalReturns);
    }
}

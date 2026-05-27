using InvestissementsDashboard.Api.Services;
using InvestissementsDashboard.Shared.Models;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace InvestissementsDashboard.Api.Tests.Services;

public class SnapshotServiceTests
{
    private static SnapshotService CreateService(Mock<IAppsScriptService> mock)
        => new(mock.Object, NullLogger<SnapshotService>.Instance);

    [Fact]
    public async Task GetLastAsync_WhenAppsScriptReturnsSnapshot_ReturnsIt()
    {
        var expected = new SnapshotDto(new DateOnly(2026, 5, 2), 72000m, 41m, 81m, 60000m, 76000m);
        var mock = new Mock<IAppsScriptService>();
        mock.Setup(s => s.CallAsync<SnapshotDto>("Snapshot", "getLast", It.IsAny<CancellationToken>()))
            .ReturnsAsync(expected);

        var result = await CreateService(mock).GetLastAsync();

        Assert.NotNull(result);
        Assert.Equal(new DateOnly(2026, 5, 2), result.Date);
        Assert.Equal(72000m, result.PortfolioTotal);
    }

    [Fact]
    public async Task GetLastAsync_WhenAppsScriptReturnsNull_ReturnsNull()
    {
        var mock = new Mock<IAppsScriptService>();
        mock.Setup(s => s.CallAsync<SnapshotDto>("Snapshot", "getLast", It.IsAny<CancellationToken>()))
            .ReturnsAsync((SnapshotDto?)null);

        var result = await CreateService(mock).GetLastAsync();

        Assert.Null(result);
    }

    [Fact]
    public async Task GetHistoryAsync_WhenAppsScriptReturnsSnapshots_ReturnsThem()
    {
        var expected = new[]
        {
            new SnapshotDto(new DateOnly(2026, 5, 1), 70000m, 40.1m, 80.2m, 60000m, 75000m),
            new SnapshotDto(new DateOnly(2026, 5, 2), 72000m, 41m,   81m,   60000m, 76000m)
        };
        var mock = new Mock<IAppsScriptService>();
        mock.Setup(s => s.CallAsync<IReadOnlyList<SnapshotDto>>("Snapshot", "getHistory", It.IsAny<CancellationToken>()))
            .ReturnsAsync(expected);

        var result = await CreateService(mock).GetHistoryAsync();

        Assert.Equal(2, result.Count);
        Assert.Equal(new DateOnly(2026, 5, 1), result[0].Date);
        Assert.Equal(new DateOnly(2026, 5, 2), result[1].Date);
    }

    [Fact]
    public async Task GetHistoryAsync_WhenAppsScriptReturnsNull_ReturnsEmptyList()
    {
        var mock = new Mock<IAppsScriptService>();
        mock.Setup(s => s.CallAsync<IReadOnlyList<SnapshotDto>>("Snapshot", "getHistory", It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyList<SnapshotDto>?)null);

        var result = await CreateService(mock).GetHistoryAsync();

        Assert.Empty(result);
    }
}

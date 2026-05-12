using InvestissementsDashboard.Client.Services;
using InvestissementsDashboard.Client.Tests.Helpers;
using InvestissementsDashboard.Client.ViewModels;
using InvestissementsDashboard.Shared.Models;
using Moq;

namespace InvestissementsDashboard.Client.Tests.ViewModels;

public class HistoryViewModelTests
{
    private static HistoryViewModel CreateVm(Mock<IPortfolioService> mock) => new(mock.Object);

    private static Mock<IPortfolioService> MockWithHistory(params SnapshotDto[] snapshots)
    {
        var mock = new Mock<IPortfolioService>();
        mock.Setup(s => s.GetHistoryAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(snapshots);
        return mock;
    }

    // ── InitializeAsync ───────────────────────────────────────────────────────

    [Fact]
    public async Task InitializeAsync_WhenHistoryIsComplete_PopulatesThreeSeries()
    {
        var mock = MockWithHistory(
            TestData.Snapshot(new DateOnly(2025, 1, 1), 10_000m, 100m, 100m),
            TestData.Snapshot(new DateOnly(2025, 1, 2), 11_000m, 110m, 105m));
        var vm = CreateVm(mock);

        await vm.InitializeAsync();

        Assert.Equal(2, vm.PortfolioSeries.Count);
        Assert.Equal(2, vm.LifeStrategySeries.Count);
        Assert.Equal(2, vm.MsciWorldSeries.Count);
        Assert.Null(vm.ErrorMessage);
        Assert.False(vm.IsLoading);
    }

    [Fact]
    public async Task InitializeAsync_WhenNoCompleteSnapshots_LeavesSeriesEmpty()
    {
        // LifeStrategy60 est null → snapshot incomplet, ignoré
        var mock = MockWithHistory(
            new SnapshotDto(new DateOnly(2025, 1, 1), 10_000m, null, 100m, null, null));
        var vm = CreateVm(mock);

        await vm.InitializeAsync();

        Assert.Empty(vm.PortfolioSeries);
        Assert.Null(vm.ErrorMessage);
    }

    [Fact]
    public async Task InitializeAsync_WhenServiceThrows_SetsErrorMessageAndLoadingFalse()
    {
        var mock = new Mock<IPortfolioService>();
        mock.Setup(s => s.GetHistoryAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new HttpRequestException("Service indisponible"));
        var vm = CreateVm(mock);

        await vm.InitializeAsync();

        Assert.NotNull(vm.ErrorMessage);
        Assert.False(vm.IsLoading);
    }

    [Fact]
    public async Task InitializeAsync_WhenAlreadyLoaded_DoesNotCallServiceAgain()
    {
        var mock = MockWithHistory(
            TestData.Snapshot(new DateOnly(2025, 1, 1), 10_000m, 100m, 100m));
        var vm = CreateVm(mock);

        await vm.InitializeAsync();
        await vm.InitializeAsync();

        mock.Verify(s => s.GetHistoryAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    // ── Indexation des séries ─────────────────────────────────────────────────

    [Fact]
    public async Task IndexSeries_FirstEntry_IsAlways100ForAllThreeSeries()
    {
        var mock = MockWithHistory(
            TestData.Snapshot(new DateOnly(2025, 1, 1), 10_000m, 100m, 200m),
            TestData.Snapshot(new DateOnly(2025, 1, 2), 12_000m, 120m, 240m));
        var vm = CreateVm(mock);

        await vm.InitializeAsync();

        Assert.Equal(100m, vm.PortfolioSeries[0].Value);
        Assert.Equal(100m, vm.LifeStrategySeries[0].Value);
        Assert.Equal(100m, vm.MsciWorldSeries[0].Value);
    }

    [Fact]
    public async Task IndexSeries_SubsequentEntries_AreRelativeToT0()
    {
        var mock = MockWithHistory(
            TestData.Snapshot(new DateOnly(2025, 1, 1), 10_000m, 100m, 100m),
            TestData.Snapshot(new DateOnly(2025, 1, 2), 11_000m, 110m, 95m));
        var vm = CreateVm(mock);

        await vm.InitializeAsync();

        Assert.Equal(110m, vm.PortfolioSeries[1].Value,    precision: 2);
        Assert.Equal(110m, vm.LifeStrategySeries[1].Value, precision: 2);
        Assert.Equal(95m,  vm.MsciWorldSeries[1].Value,    precision: 2);
    }

    [Fact]
    public async Task IndexSeries_DatesArePreserved()
    {
        var date1 = new DateOnly(2025, 1, 1);
        var date2 = new DateOnly(2025, 1, 2);
        var mock  = MockWithHistory(
            TestData.Snapshot(date1, 10_000m, 100m, 100m),
            TestData.Snapshot(date2, 11_000m, 110m, 95m));
        var vm = CreateVm(mock);

        await vm.InitializeAsync();

        Assert.Equal(date1, vm.PortfolioSeries[0].Date);
        Assert.Equal(date2, vm.PortfolioSeries[1].Date);
    }
}

using InvestissementsDashboard.Client.Services;
using InvestissementsDashboard.Client.Tests.Helpers;
using InvestissementsDashboard.Client.ViewModels;
using InvestissementsDashboard.Shared.Models;
using Moq;

namespace InvestissementsDashboard.Client.Tests.ViewModels;

public class SuiviViewModelTests
{
    private static SuiviViewModel CreateVm(Mock<IPortfolioService> mock)
    {
        var locMock = new Mock<ILocalizationService>();
        locMock.Setup(l => l.Translate(It.IsAny<string>())).Returns<string>(k => k);
        return new(mock.Object, locMock.Object);
    }

    private static Mock<IPortfolioService> MockWithHistory(params PerformancePointDto[] points)
    {
        var mock = new Mock<IPortfolioService>();
        mock.Setup(s => s.GetIndexedHistoryAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(points);
        mock.Setup(s => s.GetBondScheduleAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);
        return mock;
    }

    // ── InitializeAsync ───────────────────────────────────────────────────────

    [Fact]
    public async Task InitializeAsync_WhenHistoryIsComplete_PopulatesAllSeries()
    {
        var mock = MockWithHistory(
            TestData.PerformancePoint(new DateOnly(2025, 1, 1)),
            TestData.PerformancePoint(new DateOnly(2025, 1, 2), roic: 103m, lifeStrategy: 98m));
        var vm = CreateVm(mock);

        await vm.InitializeAsync();

        Assert.Equal(2, vm.ROIC_Series.Count);
        Assert.Equal(2, vm.LifeStrategySeries.Count);
        Assert.Equal(2, vm.MsciWorldSeries.Count);
        Assert.Null(vm.HistoryError);
        Assert.Null(vm.BondScheduleError);
        Assert.False(vm.IsLoading);
    }

    [Fact]
    public async Task InitializeAsync_WhenHistoryIsEmpty_LeavesSeriesEmpty()
    {
        var mock = MockWithHistory();
        var vm   = CreateVm(mock);

        await vm.InitializeAsync();

        Assert.Empty(vm.ROIC_Series);
        Assert.Null(vm.HistoryError);
        Assert.Null(vm.BondScheduleError);
    }

    [Fact]
    public async Task InitializeAsync_WhenHistoryServiceThrows_SetsHistoryErrorAndLoadingFalse()
    {
        var mock = new Mock<IPortfolioService>();
        mock.Setup(s => s.GetIndexedHistoryAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new HttpRequestException("Service indisponible"));
        mock.Setup(s => s.GetBondScheduleAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);
        var vm = CreateVm(mock);

        await vm.InitializeAsync();

        Assert.NotNull(vm.HistoryError);
        Assert.False(vm.IsLoading);
    }

    [Fact]
    public async Task InitializeAsync_WhenBondScheduleServiceThrows_SetsBondScheduleErrorButHistoryStillLoads()
    {
        var mock = MockWithHistory(TestData.PerformancePoint());
        mock.Setup(s => s.GetBondScheduleAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new HttpRequestException("Service indisponible"));
        var vm = CreateVm(mock);

        await vm.InitializeAsync();

        Assert.NotNull(vm.BondScheduleError);
        Assert.Null(vm.HistoryError);
        Assert.Single(vm.ROIC_Series);
        Assert.False(vm.IsLoading);
    }

    [Fact]
    public async Task InitializeAsync_WhenAlreadyLoaded_DoesNotCallServiceAgain()
    {
        var mock = MockWithHistory(TestData.PerformancePoint());
        var vm   = CreateVm(mock);

        await vm.InitializeAsync();
        await vm.InitializeAsync();

        mock.Verify(s => s.GetIndexedHistoryAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    // ── Mapping des séries ────────────────────────────────────────────────────

    [Fact]
    public async Task InitializeAsync_MapsRoicValues()
    {
        var d0 = new DateOnly(2025, 1, 1);
        var d1 = new DateOnly(2025, 1, 2);
        var mock = MockWithHistory(
            TestData.PerformancePoint(d0, roic: 100m),
            TestData.PerformancePoint(d1, roic: 110m));
        var vm = CreateVm(mock);

        await vm.InitializeAsync();

        Assert.Equal(d0,   vm.ROIC_Series[0].Date);
        Assert.Equal(100m, vm.ROIC_Series[0].Value);
        Assert.Equal(d1,   vm.ROIC_Series[1].Date);
        Assert.Equal(110m, vm.ROIC_Series[1].Value);
    }

    [Fact]
    public async Task InitializeAsync_WhenLifeStrategyIsNull_ExcludedFromSeries()
    {
        var mock = MockWithHistory(
            TestData.PerformancePoint(lifeStrategy: null));
        var vm = CreateVm(mock);

        await vm.InitializeAsync();

        Assert.Single(vm.ROIC_Series);
        Assert.Empty(vm.LifeStrategySeries);
    }

    [Fact]
    public async Task InitializeAsync_WhenMsciWorldIsNull_ExcludedFromSeries()
    {
        var mock = MockWithHistory(
            TestData.PerformancePoint(msciWorld: null));
        var vm = CreateVm(mock);

        await vm.InitializeAsync();

        Assert.Single(vm.ROIC_Series);
        Assert.Empty(vm.MsciWorldSeries);
    }

    // ── BondSchedule ──────────────────────────────────────────────────────────

    [Fact]
    public async Task InitializeAsync_PopulatesBondSchedule()
    {
        var mock = MockWithHistory(TestData.PerformancePoint());
        mock.Setup(s => s.GetBondScheduleAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync([new BondScheduleDto(2027, 1000m), new BondScheduleDto(2030, 500m)]);
        var vm = CreateVm(mock);

        await vm.InitializeAsync();

        Assert.Equal(2, vm.BondSchedule.Count);
        Assert.Equal(2027, vm.BondSchedule[0].Year);
        Assert.Equal(1000m, vm.BondSchedule[0].Amount);
    }

    [Fact]
    public async Task InitializeAsync_WhenBondScheduleIsEmpty_LeavesBondScheduleEmpty()
    {
        var mock = MockWithHistory(TestData.PerformancePoint());
        var vm   = CreateVm(mock);

        await vm.InitializeAsync();

        Assert.Empty(vm.BondSchedule);
        Assert.Null(vm.HistoryError);
        Assert.Null(vm.BondScheduleError);
    }
}

using InvestissementsDashboard.Client.Services;
using InvestissementsDashboard.Client.Tests.Helpers;
using InvestissementsDashboard.Client.ViewModels;
using InvestissementsDashboard.Shared.Models;
using Moq;

namespace InvestissementsDashboard.Client.Tests.ViewModels;

public class HistoryViewModelTests
{
    private static HistoryViewModel CreateVm(Mock<IPortfolioService> mock) => new(mock.Object);

    private static Mock<IPortfolioService> MockWithHistory(params PerformancePointDto[] points)
    {
        var mock = new Mock<IPortfolioService>();
        mock.Setup(s => s.GetIndexedHistoryAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(points);
        return mock;
    }

    // ── InitializeAsync ───────────────────────────────────────────────────────

    [Fact]
    public async Task InitializeAsync_WhenHistoryIsComplete_PopulatesThreeSeries()
    {
        var mock = MockWithHistory(
            TestData.PerformancePoint(new DateOnly(2025, 1, 1)),
            TestData.PerformancePoint(new DateOnly(2025, 1, 2), 105m, 103m, 98m));
        var vm = CreateVm(mock);

        await vm.InitializeAsync();

        Assert.Equal(2, vm.PortfolioSeries.Count);
        Assert.Equal(2, vm.LifeStrategySeries.Count);
        Assert.Equal(2, vm.MsciWorldSeries.Count);
        Assert.Null(vm.ErrorMessage);
        Assert.False(vm.IsLoading);
    }

    [Fact]
    public async Task InitializeAsync_WhenHistoryIsEmpty_LeavesSeriesEmpty()
    {
        var mock = MockWithHistory();
        var vm   = CreateVm(mock);

        await vm.InitializeAsync();

        Assert.Empty(vm.PortfolioSeries);
        Assert.Null(vm.ErrorMessage);
    }

    [Fact]
    public async Task InitializeAsync_WhenServiceThrows_SetsErrorMessageAndLoadingFalse()
    {
        var mock = new Mock<IPortfolioService>();
        mock.Setup(s => s.GetIndexedHistoryAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new HttpRequestException("Service indisponible"));
        var vm = CreateVm(mock);

        await vm.InitializeAsync();

        Assert.NotNull(vm.ErrorMessage);
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
    public async Task InitializeAsync_MapsPortfolioValues()
    {
        var d0 = new DateOnly(2025, 1, 1);
        var d1 = new DateOnly(2025, 1, 2);
        var mock = MockWithHistory(
            TestData.PerformancePoint(d0, portfolio: 100m),
            TestData.PerformancePoint(d1, portfolio: 110m));
        var vm = CreateVm(mock);

        await vm.InitializeAsync();

        Assert.Equal(d0,   vm.PortfolioSeries[0].Date);
        Assert.Equal(100m, vm.PortfolioSeries[0].Value);
        Assert.Equal(d1,   vm.PortfolioSeries[1].Date);
        Assert.Equal(110m, vm.PortfolioSeries[1].Value);
    }

    [Fact]
    public async Task InitializeAsync_WhenLifeStrategy60IsNull_ExcludedFromSeries()
    {
        var mock = MockWithHistory(
            TestData.PerformancePoint(lifeStrategy: null));
        var vm = CreateVm(mock);

        await vm.InitializeAsync();

        Assert.Single(vm.PortfolioSeries);
        Assert.Empty(vm.LifeStrategySeries);
    }

    [Fact]
    public async Task InitializeAsync_WhenMsciWorldIsNull_ExcludedFromSeries()
    {
        var mock = MockWithHistory(
            TestData.PerformancePoint(msciWorld: null));
        var vm = CreateVm(mock);

        await vm.InitializeAsync();

        Assert.Single(vm.PortfolioSeries);
        Assert.Empty(vm.MsciWorldSeries);
    }
}

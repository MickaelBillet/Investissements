using InvestissementsDashboard.Client.Services;
using InvestissementsDashboard.Client.ViewModels;
using InvestissementsDashboard.Shared.Models;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace InvestissementsDashboard.Client.Tests.ViewModels;

public class AnalysisViewModelTests
{
    private static readonly DateOnly Today = new(2026, 10, 20);
    private static readonly SynthesisReport Report = new(["Avertissement"], [], [], []);

    private readonly Mock<IPortfolioReviewRunner> _runner = new();

    private AnalysisViewModel Create(bool withRunner = true) =>
        new(NullLogger<AnalysisViewModel>.Instance, new FixedTimeProvider(Today), withRunner ? _runner.Object : null);

    [Fact]
    public void IsAvailable_WithoutRunner_IsFalse()
    {
        Assert.False(Create(withRunner: false).IsAvailable);
    }

    [Fact]
    public void IsAvailable_WithRunner_IsTrue()
    {
        Assert.True(Create().IsAvailable);
    }

    [Fact]
    public async Task LaunchAsync_WhenNoRunnerRegistered_Throws()
    {
        await Assert.ThrowsAsync<InvalidOperationException>(() => Create(withRunner: false).LaunchAsync());
    }

    [Fact]
    public async Task LaunchAsync_RunnerCompletes_ExposesTheReportAndItsDate()
    {
        _runner.Setup(r => r.RunAsync(Today, It.IsAny<CancellationToken>())).ReturnsAsync(Report);
        var vm = Create();

        await vm.LaunchAsync();

        Assert.Same(Report, vm.Report);
        Assert.Equal(Today, vm.ReportDate);
        Assert.Null(vm.Error);
        Assert.False(vm.IsRunning);
        _runner.Verify(r => r.RunAsync(Today, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task LaunchAsync_RunnerThrows_SetsErrorAndNoReport()
    {
        _runner.Setup(r => r.RunAsync(It.IsAny<DateOnly>(), It.IsAny<CancellationToken>()))
               .ThrowsAsync(new InvalidOperationException("MCP indisponible"));
        var vm = Create();

        await vm.LaunchAsync();

        Assert.Equal("MCP indisponible", vm.Error);
        Assert.Null(vm.Report);
        Assert.False(vm.IsRunning);
    }

    [Fact]
    public async Task LaunchAsync_Cancelled_LeavesNoErrorAndNoReport()
    {
        _runner.Setup(r => r.RunAsync(It.IsAny<DateOnly>(), It.IsAny<CancellationToken>()))
               .Returns<DateOnly, CancellationToken>(async (_, ct) =>
               {
                   await Task.Delay(Timeout.Infinite, ct);
                   return Report;
               });
        var vm = Create();

        var launch = vm.LaunchAsync();
        Assert.True(vm.IsRunning);
        vm.Cancel();
        await launch;

        Assert.True(vm.IsCancelled);
        Assert.Null(vm.Error);
        Assert.Null(vm.Report);
        Assert.False(vm.IsRunning);
    }

    [Fact]
    public async Task LaunchAsync_AfterACancelledRun_ClearsIsCancelled()
    {
        _runner.SetupSequence(r => r.RunAsync(It.IsAny<DateOnly>(), It.IsAny<CancellationToken>()))
               .ThrowsAsync(new OperationCanceledException())
               .ReturnsAsync(Report);
        var vm = Create();

        await vm.LaunchAsync();
        Assert.True(vm.IsCancelled);
        await vm.LaunchAsync();

        Assert.False(vm.IsCancelled);
        Assert.Same(Report, vm.Report);
    }

    [Fact]
    public async Task Cancel_AfterACompletedRun_DoesNotMarkItCancelled()
    {
        _runner.Setup(r => r.RunAsync(It.IsAny<DateOnly>(), It.IsAny<CancellationToken>())).ReturnsAsync(Report);
        var vm = Create();
        await vm.LaunchAsync();

        vm.Cancel();

        Assert.False(vm.IsCancelled);
        Assert.Same(Report, vm.Report);
    }

    [Fact]
    public async Task LaunchAsync_SecondLaunch_ClearsPreviousError()
    {
        _runner.SetupSequence(r => r.RunAsync(It.IsAny<DateOnly>(), It.IsAny<CancellationToken>()))
               .ThrowsAsync(new InvalidOperationException("échec"))
               .ReturnsAsync(Report);
        var vm = Create();

        await vm.LaunchAsync();
        await vm.LaunchAsync();

        Assert.Null(vm.Error);
        Assert.Same(Report, vm.Report);
    }

    [Fact]
    public async Task LaunchAsync_SecondLaunchFails_RemovesThePreviousReport()
    {
        _runner.SetupSequence(r => r.RunAsync(It.IsAny<DateOnly>(), It.IsAny<CancellationToken>()))
               .ReturnsAsync(Report)
               .ThrowsAsync(new InvalidOperationException("échec"));
        var vm = Create();

        await vm.LaunchAsync();
        await vm.LaunchAsync();

        Assert.Null(vm.Report);
        Assert.Null(vm.ReportDate);
    }

    private sealed class FixedTimeProvider(DateOnly date) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => new(date.ToDateTime(new TimeOnly(12, 0)), TimeSpan.Zero);

        public override TimeZoneInfo LocalTimeZone => TimeZoneInfo.Utc;
    }
}

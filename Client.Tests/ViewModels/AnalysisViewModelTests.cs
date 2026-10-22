using InvestissementsDashboard.Client.Services;
using InvestissementsDashboard.Client.ViewModels;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace InvestissementsDashboard.Client.Tests.ViewModels;

public class AnalysisViewModelTests
{
    private static readonly DateOnly Today = new(2026, 10, 20);

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
    public async Task LaunchAsync_RunnerCompletes_SetsIsCompleted()
    {
        _runner.Setup(r => r.RunAsync(Today, It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        var vm = Create();

        await vm.LaunchAsync();

        Assert.True(vm.IsCompleted);
        Assert.Null(vm.Error);
        Assert.False(vm.IsRunning);
        _runner.Verify(r => r.RunAsync(Today, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task LaunchAsync_RunnerThrows_SetsErrorAndNotCompleted()
    {
        _runner.Setup(r => r.RunAsync(It.IsAny<DateOnly>(), It.IsAny<CancellationToken>()))
               .ThrowsAsync(new InvalidOperationException("MCP indisponible"));
        var vm = Create();

        await vm.LaunchAsync();

        Assert.Equal("MCP indisponible", vm.Error);
        Assert.False(vm.IsCompleted);
        Assert.False(vm.IsRunning);
    }

    [Fact]
    public async Task LaunchAsync_Cancelled_LeavesNoErrorAndNotCompleted()
    {
        _runner.Setup(r => r.RunAsync(It.IsAny<DateOnly>(), It.IsAny<CancellationToken>()))
               .Returns<DateOnly, CancellationToken>((_, ct) => Task.Delay(Timeout.Infinite, ct));
        var vm = Create();

        var launch = vm.LaunchAsync();
        Assert.True(vm.IsRunning);
        vm.Cancel();
        await launch;

        Assert.Null(vm.Error);
        Assert.False(vm.IsCompleted);
        Assert.False(vm.IsRunning);
    }

    [Fact]
    public async Task LaunchAsync_SecondLaunch_ClearsPreviousError()
    {
        _runner.SetupSequence(r => r.RunAsync(It.IsAny<DateOnly>(), It.IsAny<CancellationToken>()))
               .ThrowsAsync(new InvalidOperationException("échec"))
               .Returns(Task.CompletedTask);
        var vm = Create();

        await vm.LaunchAsync();
        await vm.LaunchAsync();

        Assert.Null(vm.Error);
        Assert.True(vm.IsCompleted);
    }

    private sealed class FixedTimeProvider(DateOnly date) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => new(date.ToDateTime(new TimeOnly(12, 0)), TimeSpan.Zero);

        public override TimeZoneInfo LocalTimeZone => TimeZoneInfo.Utc;
    }
}

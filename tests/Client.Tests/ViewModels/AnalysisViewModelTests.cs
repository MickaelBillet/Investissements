using InvestissementsDashboard.Client.Model;
using InvestissementsDashboard.Client.Services;
using InvestissementsDashboard.Client.ViewModels;
using InvestissementsDashboard.Shared.Models;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace InvestissementsDashboard.Client.Tests.ViewModels;

public class AnalysisViewModelTests
{
    private static readonly DateOnly Today = new(2026, 10, 20);
    private static readonly SynthesisReport Report = new([], ["Avertissement"], [], [], []);

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
        _runner.Setup(r => r.RunAsync(Today, It.IsAny<IProgress<AnalysisStepUpdate>?>(), It.IsAny<CancellationToken>())).ReturnsAsync(Report);
        var vm = Create();

        await vm.LaunchAsync();

        Assert.Same(Report, vm.Report);
        Assert.Equal(Today, vm.ReportDate);
        Assert.Null(vm.Error);
        Assert.False(vm.IsRunning);
        _runner.Verify(r => r.RunAsync(Today, It.IsAny<IProgress<AnalysisStepUpdate>?>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task LaunchAsync_RunnerThrows_SetsErrorAndNoReport()
    {
        _runner.Setup(r => r.RunAsync(It.IsAny<DateOnly>(), It.IsAny<IProgress<AnalysisStepUpdate>?>(), It.IsAny<CancellationToken>()))
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
        _runner.Setup(r => r.RunAsync(It.IsAny<DateOnly>(), It.IsAny<IProgress<AnalysisStepUpdate>?>(), It.IsAny<CancellationToken>()))
               .Returns<DateOnly, IProgress<AnalysisStepUpdate>?, CancellationToken>(async (_, _, ct) =>
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
        _runner.SetupSequence(r => r.RunAsync(It.IsAny<DateOnly>(), It.IsAny<IProgress<AnalysisStepUpdate>?>(), It.IsAny<CancellationToken>()))
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
        _runner.Setup(r => r.RunAsync(It.IsAny<DateOnly>(), It.IsAny<IProgress<AnalysisStepUpdate>?>(), It.IsAny<CancellationToken>())).ReturnsAsync(Report);
        var vm = Create();
        await vm.LaunchAsync();

        vm.Cancel();

        Assert.False(vm.IsCancelled);
        Assert.Same(Report, vm.Report);
    }

    [Fact]
    public async Task LaunchAsync_SecondLaunch_ClearsPreviousError()
    {
        _runner.SetupSequence(r => r.RunAsync(It.IsAny<DateOnly>(), It.IsAny<IProgress<AnalysisStepUpdate>?>(), It.IsAny<CancellationToken>()))
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
        _runner.SetupSequence(r => r.RunAsync(It.IsAny<DateOnly>(), It.IsAny<IProgress<AnalysisStepUpdate>?>(), It.IsAny<CancellationToken>()))
               .ReturnsAsync(Report)
               .ThrowsAsync(new InvalidOperationException("échec"));
        var vm = Create();

        await vm.LaunchAsync();
        await vm.LaunchAsync();

        Assert.Null(vm.Report);
        Assert.Null(vm.ReportDate);
    }

    [Fact]
    public void Steps_BeforeAnyRun_AreAllPendingInDisplayOrder()
    {
        var vm = Create();

        Assert.Equal([AnalysisStep.Allocation, AnalysisStep.Distribution, AnalysisStep.Context, AnalysisStep.Synthesis], vm.Steps.Select(s => s.Step));
        Assert.All(vm.Steps, s => Assert.Equal(AnalysisStepStatus.Pending, s.Status));
        Assert.False(vm.ShowSteps);
    }

    [Fact]
    public async Task LaunchAsync_RunnerReportsAStep_UpdatesOnlyThatStepAndNotifies()
    {
        var pending = new TaskCompletionSource<SynthesisReport>();
        IProgress<AnalysisStepUpdate>? progress = null;
        SetupRunner(pending, p => progress = p);
        var vm = Create();
        var notifications = 0;
        vm.StepsChanged += () => notifications++;

        var launch = vm.LaunchAsync();
        progress!.Report(new AnalysisStepUpdate(AnalysisStep.Context, AnalysisStepStatus.Running));

        Assert.Equal(1, notifications);
        Assert.True(vm.ShowSteps);
        Assert.Equal(AnalysisStepStatus.Running, vm.Steps.Single(s => s.Step == AnalysisStep.Context).Status);
        Assert.Equal(AnalysisStepStatus.Pending, vm.Steps.Single(s => s.Step == AnalysisStep.Synthesis).Status);

        pending.SetResult(Report);
        await launch;
    }

    [Fact]
    public async Task LaunchAsync_RunnerCompletes_HidesTheSteps()
    {
        _runner.Setup(r => r.RunAsync(It.IsAny<DateOnly>(), It.IsAny<IProgress<AnalysisStepUpdate>?>(), It.IsAny<CancellationToken>())).ReturnsAsync(Report);
        var vm = Create();

        await vm.LaunchAsync();

        Assert.False(vm.ShowSteps);
    }

    [Fact]
    public async Task LaunchAsync_StepFails_ShowsItAndStopsTheStepsStillRunning()
    {
        var pending = new TaskCompletionSource<SynthesisReport>();
        IProgress<AnalysisStepUpdate>? progress = null;
        SetupRunner(pending, p => progress = p);
        var vm = Create();

        var launch = vm.LaunchAsync();
        progress!.Report(new AnalysisStepUpdate(AnalysisStep.Allocation, AnalysisStepStatus.Done));
        progress.Report(new AnalysisStepUpdate(AnalysisStep.Context, AnalysisStepStatus.Running));
        progress.Report(new AnalysisStepUpdate(AnalysisStep.Distribution, AnalysisStepStatus.Failed));
        pending.SetException(new InvalidOperationException("échec"));
        await launch;

        Assert.True(vm.ShowSteps);
        Assert.Equal(AnalysisStepStatus.Done, vm.Steps.Single(s => s.Step == AnalysisStep.Allocation).Status);
        Assert.Equal(AnalysisStepStatus.Failed, vm.Steps.Single(s => s.Step == AnalysisStep.Distribution).Status);
        Assert.Equal(AnalysisStepStatus.Pending, vm.Steps.Single(s => s.Step == AnalysisStep.Context).Status);
    }

    [Fact]
    public async Task LaunchAsync_FailsBeforeAnyStep_HidesTheSteps()
    {
        _runner.Setup(r => r.RunAsync(It.IsAny<DateOnly>(), It.IsAny<IProgress<AnalysisStepUpdate>?>(), It.IsAny<CancellationToken>()))
               .ThrowsAsync(new InvalidOperationException("mot de passe refusé"));
        var vm = Create();

        await vm.LaunchAsync();

        Assert.NotNull(vm.Error);
        Assert.False(vm.ShowSteps);
    }

    [Fact]
    public async Task LaunchAsync_Cancelled_HidesTheSteps()
    {
        _runner.Setup(r => r.RunAsync(It.IsAny<DateOnly>(), It.IsAny<IProgress<AnalysisStepUpdate>?>(), It.IsAny<CancellationToken>()))
               .ThrowsAsync(new OperationCanceledException());
        var vm = Create();

        await vm.LaunchAsync();

        Assert.False(vm.ShowSteps);
    }

    [Fact]
    public async Task LaunchAsync_SecondLaunch_ResetsTheSteps()
    {
        var first = new TaskCompletionSource<SynthesisReport>();
        IProgress<AnalysisStepUpdate>? progress = null;
        SetupRunner(first, p => progress = p);
        var vm = Create();
        var launch = vm.LaunchAsync();
        progress!.Report(new AnalysisStepUpdate(AnalysisStep.Allocation, AnalysisStepStatus.Done));
        first.SetResult(Report);
        await launch;

        var second = new TaskCompletionSource<SynthesisReport>();
        SetupRunner(second, _ => { });
        launch = vm.LaunchAsync();

        Assert.All(vm.Steps, s => Assert.Equal(AnalysisStepStatus.Pending, s.Status));

        second.SetResult(Report);
        await launch;
    }

    [Fact]
    public async Task LaunchAsync_StepEnds_ExposesItsDuration()
    {
        var clock = new SteppingTimeProvider();
        var pending = new TaskCompletionSource<SynthesisReport>();
        IProgress<AnalysisStepUpdate>? progress = null;
        SetupRunner(pending, p => progress = p);
        var vm = new AnalysisViewModel(NullLogger<AnalysisViewModel>.Instance, clock, _runner.Object);

        var launch = vm.LaunchAsync();
        progress!.Report(new AnalysisStepUpdate(AnalysisStep.Synthesis, AnalysisStepStatus.Running));
        clock.Advance(TimeSpan.FromSeconds(10));
        // The synthesis is announced again for each input it receives: this must not restart its duration.
        progress.Report(new AnalysisStepUpdate(AnalysisStep.Synthesis, AnalysisStepStatus.Running));
        Assert.Null(vm.Steps.Single(s => s.Step == AnalysisStep.Synthesis).Duration);
        clock.Advance(TimeSpan.FromSeconds(50));
        progress.Report(new AnalysisStepUpdate(AnalysisStep.Synthesis, AnalysisStepStatus.Done));

        Assert.Equal(TimeSpan.FromSeconds(60), vm.Steps.Single(s => s.Step == AnalysisStep.Synthesis).Duration);

        pending.SetResult(Report);
        await launch;
    }

    [Fact]
    public async Task LaunchAsync_StepEndsWithoutHavingStarted_HasNoDuration()
    {
        var pending = new TaskCompletionSource<SynthesisReport>();
        IProgress<AnalysisStepUpdate>? progress = null;
        SetupRunner(pending, p => progress = p);
        var vm = Create();

        var launch = vm.LaunchAsync();
        progress!.Report(new AnalysisStepUpdate(AnalysisStep.Context, AnalysisStepStatus.Done));

        Assert.Null(vm.Steps.Single(s => s.Step == AnalysisStep.Context).Duration);

        pending.SetResult(Report);
        await launch;
    }

    [Fact]
    public async Task LaunchAsync_SecondLaunch_MeasuresDurationsAfresh()
    {
        var clock = new SteppingTimeProvider();
        var first = new TaskCompletionSource<SynthesisReport>();
        IProgress<AnalysisStepUpdate>? progress = null;
        SetupRunner(first, p => progress = p);
        var vm = new AnalysisViewModel(NullLogger<AnalysisViewModel>.Instance, clock, _runner.Object);
        var launch = vm.LaunchAsync();
        progress!.Report(new AnalysisStepUpdate(AnalysisStep.Context, AnalysisStepStatus.Running));
        first.SetResult(Report);
        await launch;
        clock.Advance(TimeSpan.FromMinutes(5));

        var second = new TaskCompletionSource<SynthesisReport>();
        SetupRunner(second, p => progress = p);
        launch = vm.LaunchAsync();
        progress!.Report(new AnalysisStepUpdate(AnalysisStep.Context, AnalysisStepStatus.Running));
        clock.Advance(TimeSpan.FromSeconds(3));
        progress.Report(new AnalysisStepUpdate(AnalysisStep.Context, AnalysisStepStatus.Done));

        Assert.Equal(TimeSpan.FromSeconds(3), vm.Steps.Single(s => s.Step == AnalysisStep.Context).Duration);

        second.SetResult(Report);
        await launch;
    }

    private void SetupRunner(TaskCompletionSource<SynthesisReport> result, Action<IProgress<AnalysisStepUpdate>?> onProgress) =>
        _runner.Setup(r => r.RunAsync(It.IsAny<DateOnly>(), It.IsAny<IProgress<AnalysisStepUpdate>?>(), It.IsAny<CancellationToken>()))
               .Callback<DateOnly, IProgress<AnalysisStepUpdate>?, CancellationToken>((_, p, _) => onProgress(p))
               .Returns(result.Task);

    private sealed class SteppingTimeProvider : TimeProvider
    {
        private long _ticks;

        public override long TimestampFrequency => TimeSpan.TicksPerSecond;

        public override long GetTimestamp() => _ticks;

        public void Advance(TimeSpan duration) => _ticks += duration.Ticks;
    }

    private sealed class FixedTimeProvider(DateOnly date) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => new(date.ToDateTime(new TimeOnly(12, 0)), TimeSpan.Zero);

        public override TimeZoneInfo LocalTimeZone => TimeZoneInfo.Utc;
    }
}

using InvestissementsDashboard.Client.Services;
using InvestissementsDashboard.Client.ViewModels;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace InvestissementsDashboard.Client.Tests.ViewModels;

public class AgentViewModelTests
{
    private readonly Mock<IAgentRunner> _runner = new();

    private AgentViewModel Create(bool withRunner = true) =>
        new(NullLogger<AgentViewModel>.Instance, withRunner ? _runner.Object : null);

    [Fact]
    public async Task LaunchAsync_WhenNoRunnerRegistered_Throws()
    {
        var vm = Create(withRunner: false);

        await Assert.ThrowsAsync<InvalidOperationException>(() => vm.LaunchAsync(AgentChoice.Stock, "Air Liquide"));
    }

    [Fact]
    public async Task LaunchAsync_WhenRunnerReplies_SetsResponseAndResetsIsRunning()
    {
        _runner.Setup(r => r.RunAsync(AgentChoice.News, "Air Liquide", It.IsAny<CancellationToken>()))
               .ReturnsAsync("Réponse");
        var vm = Create();

        await vm.LaunchAsync(AgentChoice.News, "Air Liquide");

        Assert.Equal("Réponse", vm.Response);
        Assert.Null(vm.Error);
        Assert.False(vm.IsRunning);
    }

    [Fact]
    public async Task LaunchAsync_WhileRunnerRuns_IsRunningIsTrue()
    {
        var pending = new TaskCompletionSource<string>();
        _runner.Setup(r => r.RunAsync(It.IsAny<AgentChoice>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
               .Returns(pending.Task);
        var vm = Create();

        var run = vm.LaunchAsync(AgentChoice.Stock, "Air Liquide");

        Assert.True(vm.IsRunning);
        pending.SetResult("ok");
        await run;
        Assert.False(vm.IsRunning);
    }

    [Fact]
    public async Task LaunchAsync_WhenRunnerThrows_SetsErrorAndClearsResponse()
    {
        _runner.Setup(r => r.RunAsync(It.IsAny<AgentChoice>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
               .ThrowsAsync(new InvalidOperationException("boom"));
        var vm = Create();

        await vm.LaunchAsync(AgentChoice.Stock, "Air Liquide");

        Assert.Equal("boom", vm.Error);
        Assert.Null(vm.Response);
        Assert.False(vm.IsRunning);
    }

    [Fact]
    public async Task LaunchAsync_AfterFailure_ResetsPreviousError()
    {
        _runner.SetupSequence(r => r.RunAsync(It.IsAny<AgentChoice>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
               .ThrowsAsync(new InvalidOperationException("boom"))
               .ReturnsAsync("ok");
        var vm = Create();

        await vm.LaunchAsync(AgentChoice.Stock, "A");
        await vm.LaunchAsync(AgentChoice.Stock, "B");

        Assert.Null(vm.Error);
        Assert.Equal("ok", vm.Response);
    }

    [Fact]
    public async Task Cancel_WhileRunnerRuns_StopsWithoutErrorOrResponse()
    {
        _runner.Setup(r => r.RunAsync(It.IsAny<AgentChoice>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
               .Returns<AgentChoice, string, CancellationToken>(async (_, _, ct) =>
               {
                   await Task.Delay(Timeout.Infinite, ct);
                   return "never";
               });
        var vm = Create();

        var run = vm.LaunchAsync(AgentChoice.Stock, "Air Liquide");
        vm.Cancel();
        await run;

        Assert.Null(vm.Error);
        Assert.Null(vm.Response);
        Assert.False(vm.IsRunning);
    }
}

using Bunit;
using InvestissementsDashboard.Client.Services;
using InvestissementsDashboard.Client.Tests.Helpers;
using InvestissementsDashboard.Client.ViewModels;
using InvestissementsDashboard.Client.Views;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using MudBlazor.Services;

namespace InvestissementsDashboard.Client.Tests.Components;

public class AnalysisTests : BunitContext, IAsyncLifetime
{
    // MudBlazor's services only implement IAsyncDisposable: dispose asynchronously.
    public Task InitializeAsync() => Task.CompletedTask;

    Task IAsyncLifetime.DisposeAsync() => base.DisposeAsync().AsTask();

    private readonly Mock<IPortfolioReviewRunner> _runner = new();

    public AnalysisTests()
    {
        Services.AddMudServices(opt => opt.PopoverOptions.CheckForPopoverProvider = false);
        Services.AddLocalizationMock();
        JSInterop.Mode = JSRuntimeMode.Loose;
    }

    private void RegisterViewModel(bool withRunner) =>
        Services.AddSingleton(new AnalysisViewModel(NullLogger<AnalysisViewModel>.Instance, TimeProvider.System, withRunner ? _runner.Object : null));

    [Fact]
    public void Analysis_OnOpen_StartsTheWorkflowOnceWithoutALaunchButton()
    {
        _runner.Setup(r => r.RunAsync(It.IsAny<DateOnly>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        RegisterViewModel(withRunner: true);

        var cut = Render<Analysis>();

        _runner.Verify(r => r.RunAsync(It.IsAny<DateOnly>(), It.IsAny<CancellationToken>()), Times.Once);
        Assert.Empty(cut.FindAll("button"));
        cut.WaitForAssertion(() => Assert.Contains(TestData.Translate("Analysis_Done"), cut.Markup));
        Assert.Empty(cut.FindAll("pre"));
    }

    [Fact]
    public void Analysis_WhileRunning_ShowsProgress()
    {
        var pending = new TaskCompletionSource();
        _runner.Setup(r => r.RunAsync(It.IsAny<DateOnly>(), It.IsAny<CancellationToken>())).Returns(pending.Task);
        RegisterViewModel(withRunner: true);

        var cut = Render<Analysis>();

        Assert.Contains(TestData.Translate("Analysis_Running"), cut.Markup);
        Assert.DoesNotContain(TestData.Translate("Analysis_Done"), cut.Markup);
        pending.SetResult();
    }

    [Fact]
    public void Analysis_WithoutRunner_RendersNothingAndDoesNotThrow()
    {
        RegisterViewModel(withRunner: false);

        var cut = Render<Analysis>();

        Assert.DoesNotContain(TestData.Translate("Analysis_Running"), cut.Markup);
    }
}

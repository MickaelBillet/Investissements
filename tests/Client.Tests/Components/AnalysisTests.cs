using Bunit;
using InvestissementsDashboard.Client.Services;
using InvestissementsDashboard.Client.Tests.Helpers;
using InvestissementsDashboard.Client.ViewModels;
using InvestissementsDashboard.Client.Views;
using InvestissementsDashboard.Shared.Models;
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

    private static readonly SynthesisReport Report = new(["Concentration sur la technologie"], [], [], []);

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
    public void Analysis_OnOpen_DoesNotStartTheWorkflow()
    {
        RegisterViewModel(withRunner: true);

        var cut = Render<Analysis>();

        _runner.Verify(r => r.RunAsync(It.IsAny<DateOnly>(), It.IsAny<CancellationToken>()), Times.Never);
        Assert.Contains(TestData.Translate("Analysis_Launch"), cut.Find("button").TextContent);
    }

    [Fact]
    public void Analysis_ClickLaunch_RunsOnceAndShowsTheReport()
    {
        _runner.Setup(r => r.RunAsync(It.IsAny<DateOnly>(), It.IsAny<CancellationToken>())).ReturnsAsync(Report);
        RegisterViewModel(withRunner: true);
        var cut = Render<Analysis>();

        cut.Find("button").Click();

        _runner.Verify(r => r.RunAsync(It.IsAny<DateOnly>(), It.IsAny<CancellationToken>()), Times.Once);
        cut.WaitForAssertion(() => Assert.Contains("Concentration sur la technologie", cut.Markup));
    }

    [Fact]
    public void Analysis_WhileRunning_ShowsProgressAndDisablesTheButton()
    {
        var pending = new TaskCompletionSource<SynthesisReport>();
        _runner.Setup(r => r.RunAsync(It.IsAny<DateOnly>(), It.IsAny<CancellationToken>())).Returns(pending.Task);
        RegisterViewModel(withRunner: true);
        var cut = Render<Analysis>();

        cut.Find("button").Click();

        Assert.Contains(TestData.Translate("Analysis_Running"), cut.Markup);
        Assert.True(cut.Find("button").HasAttribute("disabled"));
        pending.SetResult(Report);
    }

    [Fact]
    public void Analysis_NotRunning_HasNoCancelButton()
    {
        RegisterViewModel(withRunner: true);

        var cut = Render<Analysis>();

        Assert.DoesNotContain(cut.FindAll("button"), b => b.TextContent.Contains(TestData.Translate("Analysis_Cancel")));
    }

    [Fact]
    public void Analysis_ClickCancel_InterruptsTheRunAndAllowsARestart()
    {
        CancellationToken received = default;
        _runner.Setup(r => r.RunAsync(It.IsAny<DateOnly>(), It.IsAny<CancellationToken>()))
               .Returns<DateOnly, CancellationToken>(async (_, ct) =>
               {
                   received = ct;
                   await Task.Delay(Timeout.Infinite, ct);
                   return Report;
               });
        RegisterViewModel(withRunner: true);
        var cut = Render<Analysis>();
        cut.Find("button").Click();

        cut.FindAll("button").Single(b => b.TextContent.Contains(TestData.Translate("Analysis_Cancel"))).Click();

        cut.WaitForAssertion(() =>
        {
            Assert.True(received.IsCancellationRequested);
            Assert.Contains(TestData.Translate("Analysis_Cancelled"), cut.Markup);
            Assert.DoesNotContain(TestData.Translate("Analysis_Running"), cut.Markup);
            var launch = Assert.Single(cut.FindAll("button"));
            Assert.False(launch.HasAttribute("disabled"));
        });
    }

    [Fact]
    public void Analysis_RunnerFails_ShowsTheError()
    {
        _runner.Setup(r => r.RunAsync(It.IsAny<DateOnly>(), It.IsAny<CancellationToken>()))
               .ThrowsAsync(new InvalidOperationException("contexte vide"));
        RegisterViewModel(withRunner: true);
        var cut = Render<Analysis>();

        cut.Find("button").Click();

        cut.WaitForAssertion(() =>
        {
            Assert.Contains(TestData.Translate("Analysis_Error"), cut.Markup);
            Assert.Contains("contexte vide", cut.Markup);
        });
    }

    [Fact]
    public async Task Analysis_ReopenedAfterAReport_KeepsItWithoutRunningAgain()
    {
        _runner.Setup(r => r.RunAsync(It.IsAny<DateOnly>(), It.IsAny<CancellationToken>())).ReturnsAsync(Report);
        RegisterViewModel(withRunner: true);
        var first = Render<Analysis>();
        first.Find("button").Click();
        first.WaitForAssertion(() => Assert.Contains("Concentration sur la technologie", first.Markup));
        await DisposeComponentsAsync();

        var reopened = Render<Analysis>();

        Assert.Contains("Concentration sur la technologie", reopened.Markup);
        _runner.Verify(r => r.RunAsync(It.IsAny<DateOnly>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public void Analysis_WithoutRunner_RendersNothingAndDoesNotThrow()
    {
        RegisterViewModel(withRunner: false);

        var cut = Render<Analysis>();

        Assert.Empty(cut.FindAll("button"));
        Assert.DoesNotContain(TestData.Translate("Analysis_Running"), cut.Markup);
    }
}

using Bunit;
using InvestissementsDashboard.Client.Model;
using InvestissementsDashboard.Client.Shared;
using InvestissementsDashboard.Client.Tests.Helpers;
using MudBlazor.Services;

namespace InvestissementsDashboard.Client.Tests.Components;

public class AnalysisStepListTests : BunitContext, IAsyncLifetime
{
    // MudBlazor's services only implement IAsyncDisposable: dispose asynchronously.
    public Task InitializeAsync() => Task.CompletedTask;

    Task IAsyncLifetime.DisposeAsync() => base.DisposeAsync().AsTask();

    public AnalysisStepListTests()
    {
        Services.AddMudServices(opt => opt.PopoverOptions.CheckForPopoverProvider = false);
        Services.AddLocalizationMock();
        JSInterop.Mode = JSRuntimeMode.Loose;
    }

    private IRenderedComponent<AnalysisStepList> RenderSteps(bool isRunning, params AnalysisStepState[] steps) =>
        Render<AnalysisStepList>(p => p.Add(c => c.Steps, steps).Add(c => c.IsRunning, isRunning));

    private IRenderedComponent<AnalysisStepList> RenderStep(AnalysisStep step, AnalysisStepStatus status, TimeSpan? duration = null) =>
        RenderSteps(true, new AnalysisStepState(step, status, duration));

    [Theory]
    [InlineData(AnalysisStep.Allocation)]
    [InlineData(AnalysisStep.Distribution)]
    [InlineData(AnalysisStep.Context)]
    [InlineData(AnalysisStep.Synthesis)]
    public void AnalysisStepList_AnyStep_ShowsItsTranslatedLabel(AnalysisStep step)
    {
        var key = $"Analysis_Step_{step}";

        var cut = RenderStep(step, AnalysisStepStatus.Pending);

        Assert.NotEqual(key, TestData.Translate(key));
        Assert.Contains(TestData.Translate(key), cut.Markup);
    }

    [Fact]
    public void AnalysisStepList_RunningStep_ShowsASpinnerAndItsStatus()
    {
        var cut = RenderStep(AnalysisStep.Context, AnalysisStepStatus.Running);

        Assert.Single(cut.FindAll(".mud-progress-circular"));
        Assert.Contains(TestData.Translate("Analysis_StepStatus_Running"), cut.Markup);
    }

    [Theory]
    [InlineData(AnalysisStepStatus.Pending, "Analysis_StepStatus_Pending")]
    [InlineData(AnalysisStepStatus.Done, "Analysis_StepStatus_Done")]
    [InlineData(AnalysisStepStatus.Failed, "Analysis_StepStatus_Failed")]
    public void AnalysisStepList_FinishedOrWaitingStep_NamesItsStatusWithoutSpinner(AnalysisStepStatus status, string key)
    {
        var cut = RenderStep(AnalysisStep.Context, status);

        Assert.Contains(TestData.Translate(key), cut.Markup);
        Assert.Empty(cut.FindAll(".mud-progress-circular"));
    }

    [Fact]
    public void AnalysisStepList_StepWithADuration_ShowsIt()
    {
        var cut = RenderStep(AnalysisStep.Context, AnalysisStepStatus.Done, TimeSpan.FromSeconds(78));

        Assert.Contains("1 min 18 s", cut.Markup);
    }

    [Fact]
    public void AnalysisStepList_StepsOfBothPhases_ShowsCollectionBeforeWriting()
    {
        var cut = RenderSteps(
            true,
            new AnalysisStepState(AnalysisStep.Allocation, AnalysisStepStatus.Pending),
            new AnalysisStepState(AnalysisStep.Synthesis, AnalysisStepStatus.Pending));

        var collection = cut.Markup.IndexOf(TestData.Translate("Analysis_Phase_Collection"), StringComparison.Ordinal);
        var allocation = cut.Markup.IndexOf(TestData.Translate("Analysis_Step_Allocation"), StringComparison.Ordinal);
        var writing = cut.Markup.IndexOf(TestData.Translate("Analysis_Phase_Writing"), StringComparison.Ordinal);
        var synthesis = cut.Markup.IndexOf(TestData.Translate("Analysis_Step_Synthesis"), StringComparison.Ordinal);
        Assert.True(collection >= 0 && collection < allocation && allocation < writing && writing < synthesis);
    }

    [Theory]
    [InlineData(true, "Analysis_Steps_Running")]
    [InlineData(false, "Analysis_Steps_Stopped")]
    public void AnalysisStepList_RunState_ShowsTheMatchingTitle(bool isRunning, string key)
    {
        var cut = RenderSteps(isRunning, new AnalysisStepState(AnalysisStep.Context, AnalysisStepStatus.Pending));

        Assert.Contains(TestData.Translate(key), cut.Markup);
    }
}

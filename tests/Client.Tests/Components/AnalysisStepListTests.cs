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

    private IRenderedComponent<AnalysisStepList> RenderStep(AnalysisStep step, AnalysisStepStatus status) =>
        Render<AnalysisStepList>(p => p.Add(c => c.Steps, [new AnalysisStepUpdate(step, status)]));

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
    public void AnalysisStepList_RunningStep_ShowsASpinner()
    {
        var cut = RenderStep(AnalysisStep.Context, AnalysisStepStatus.Running);

        Assert.Single(cut.FindAll(".mud-progress-circular"));
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
}

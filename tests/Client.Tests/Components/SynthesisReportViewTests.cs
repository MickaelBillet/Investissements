using Bunit;
using InvestissementsDashboard.Client.Shared;
using InvestissementsDashboard.Client.Tests.Helpers;
using InvestissementsDashboard.Shared.Models;
using MudBlazor.Services;

namespace InvestissementsDashboard.Client.Tests.Components;

public class SynthesisReportViewTests : BunitContext, IAsyncLifetime
{
    // MudBlazor's services only implement IAsyncDisposable: dispose asynchronously.
    public Task InitializeAsync() => Task.CompletedTask;

    Task IAsyncLifetime.DisposeAsync() => base.DisposeAsync().AsTask();

    public SynthesisReportViewTests()
    {
        Services.AddMudServices(opt => opt.PopoverOptions.CheckForPopoverProvider = false);
        Services.AddLocalizationMock();
        JSInterop.Mode = JSRuntimeMode.Loose;
    }

    private static Recommendation Recommendation(
        RecommendationLevel level = RecommendationLevel.Theme,
        string target = "Défense",
        RecommendationDirection direction = RecommendationDirection.Reinforce,
        RecommendationConfidence confidence = RecommendationConfidence.Medium) =>
        new(level, target, direction, "S3 — Stagflation", "Les budgets militaires progressent.", confidence);

    private IRenderedComponent<SynthesisReportView> RenderReport(SynthesisReport report) =>
        Render<SynthesisReportView>(p => p.Add(c => c.Report, report));

    [Fact]
    public void SynthesisReportView_FullReport_ShowsEverySectionAndItsContent()
    {
        var cut = RenderReport(new SynthesisReport([], ["Un avertissement"], ["Une force"], ["Une faiblesse"], [Recommendation()]));

        foreach (var key in new[] { "Analysis_Warnings", "Analysis_Recommendations", "Analysis_Strengths", "Analysis_Weaknesses" })
        {
            Assert.Contains(TestData.Translate(key), cut.Markup);
        }

        Assert.Contains("Un avertissement", cut.Markup);
        Assert.Contains("Une force", cut.Markup);
        Assert.Contains("Une faiblesse", cut.Markup);
        Assert.Contains("S3 — Stagflation", cut.Markup);
        Assert.Contains("Les budgets militaires progressent.", cut.Markup);
    }

    [Theory]
    [InlineData(ScenarioVerdict.Confirmed)]
    [InlineData(ScenarioVerdict.Weakened)]
    [InlineData(ScenarioVerdict.Neutral)]
    public void SynthesisReportView_Scenario_ShowsItsTranslatedVerdictAndContent(ScenarioVerdict verdict)
    {
        var key = $"Analysis_Verdict_{verdict}";
        var cut = RenderReport(new SynthesisReport([new ScenarioAssessment("S3 — Stagflation", verdict, "Brent au-dessus de 100 $", "Pèse sur la duration")], [], [], [], []));

        Assert.NotEqual(key, TestData.Translate(key));
        Assert.Contains(TestData.Translate(key), cut.Markup);
        Assert.Contains(TestData.Translate("Analysis_Scenarios"), cut.Markup);
        Assert.Contains("S3 — Stagflation", cut.Markup);
        Assert.Contains("Brent au-dessus de 100 $", cut.Markup);
        Assert.Contains("Pèse sur la duration", cut.Markup);
    }

    [Fact]
    public void SynthesisReportView_Scenarios_ComeBeforeTheWarningsInTheirOrder()
    {
        var cut = RenderReport(new SynthesisReport(
            [new ScenarioAssessment("S1", ScenarioVerdict.Neutral, "a", "b"), new ScenarioAssessment("S2", ScenarioVerdict.Neutral, "c", "d")],
            ["Un avertissement"], [], [], []));

        var first = cut.Markup.IndexOf(">S1<", StringComparison.Ordinal);
        var second = cut.Markup.IndexOf(">S2<", StringComparison.Ordinal);
        Assert.InRange(first, 0, second);
        Assert.True(second < cut.Markup.IndexOf("Un avertissement", StringComparison.Ordinal));
    }

    [Fact]
    public void SynthesisReportView_EmptySections_AreHidden()
    {
        var cut = RenderReport(new SynthesisReport([], ["Un avertissement"], [], [], []));

        Assert.Contains(TestData.Translate("Analysis_Warnings"), cut.Markup);
        Assert.DoesNotContain(TestData.Translate("Analysis_Scenarios"), cut.Markup);
        Assert.DoesNotContain(TestData.Translate("Analysis_Strengths"), cut.Markup);
        Assert.DoesNotContain(TestData.Translate("Analysis_Weaknesses"), cut.Markup);
        Assert.DoesNotContain(TestData.Translate("Analysis_Recommendations"), cut.Markup);
    }

    [Fact]
    public void SynthesisReportView_Recommendations_KeepTheirOrder()
    {
        var cut = RenderReport(new SynthesisReport([], [], [], [], [Recommendation(target: "Première"), Recommendation(target: "Seconde")]));

        Assert.True(cut.Markup.IndexOf("Première", StringComparison.Ordinal) < cut.Markup.IndexOf("Seconde", StringComparison.Ordinal));
    }

    [Fact]
    public void SynthesisReportView_AssetClassTarget_IsTranslated()
    {
        var cut = RenderReport(new SynthesisReport([], [], [], [], [Recommendation(RecommendationLevel.AssetClass, "Stocks")]));

        Assert.Contains(TestData.Translate("Stocks"), cut.Markup);
    }

    [Fact]
    public void SynthesisReportView_LineTarget_IsShownAsIs()
    {
        var cut = RenderReport(new SynthesisReport([], [], [], [], [Recommendation(RecommendationLevel.Line, "Stocks")]));

        Assert.DoesNotContain(TestData.Translate("Stocks"), cut.Markup);
        Assert.Contains("Stocks", cut.Markup);
    }

    [Theory]
    [InlineData(RecommendationDirection.Reinforce)]
    [InlineData(RecommendationDirection.Reduce)]
    [InlineData(RecommendationDirection.Initiate)]
    [InlineData(RecommendationDirection.Watch)]
    public void SynthesisReportView_Direction_IsTranslated(RecommendationDirection direction)
    {
        var key = $"Analysis_Direction_{direction}";
        var cut = RenderReport(new SynthesisReport([], [], [], [], [Recommendation(direction: direction)]));

        Assert.NotEqual(key, TestData.Translate(key));
        Assert.Contains(TestData.Translate(key), cut.Markup);
    }

    [Theory]
    [InlineData(RecommendationLevel.AssetClass)]
    [InlineData(RecommendationLevel.Theme)]
    [InlineData(RecommendationLevel.Line)]
    public void SynthesisReportView_Level_IsTranslated(RecommendationLevel level)
    {
        var key = $"Analysis_Level_{level}";
        var cut = RenderReport(new SynthesisReport([], [], [], [], [Recommendation(level)]));

        Assert.NotEqual(key, TestData.Translate(key));
        Assert.Contains(TestData.Translate(key), cut.Markup);
    }

    [Theory]
    [InlineData(RecommendationConfidence.Low)]
    [InlineData(RecommendationConfidence.Medium)]
    [InlineData(RecommendationConfidence.High)]
    public void SynthesisReportView_Confidence_IsTranslated(RecommendationConfidence confidence)
    {
        var key = $"Analysis_Confidence_{confidence}";
        var cut = RenderReport(new SynthesisReport([], [], [], [], [Recommendation(confidence: confidence)]));

        Assert.NotEqual(key, TestData.Translate(key));
        Assert.Contains(TestData.Translate(key), cut.Markup);
    }
}

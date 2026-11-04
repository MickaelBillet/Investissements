using Bunit;
using InvestissementsDashboard.Client.Model;
using InvestissementsDashboard.Client.Shared;
using InvestissementsDashboard.Client.Tests.Helpers;
using Microsoft.Extensions.DependencyInjection;
using MudBlazor.Services;

namespace InvestissementsDashboard.Client.Tests.Components;

public class HistoryChartTests : BunitContext
{
    public HistoryChartTests()
    {
        Services.AddMudServices(opt => opt.PopoverOptions.CheckForPopoverProvider = false);
        Services.AddLocalizationMock();
        JSInterop.Mode = JSRuntimeMode.Loose;
    }

    [Fact]
    public void HistoryChart_WhenSeriesHavePoints_AppendsLastValueToLegendName()
    {
        var roic = new[] { new IndexedPoint(new DateOnly(2026, 1, 1), 100m), new IndexedPoint(new DateOnly(2026, 2, 1), 112.44m) };
        var lifeStrategy = new[] { new IndexedPoint(new DateOnly(2026, 1, 1), 100m), new IndexedPoint(new DateOnly(2026, 2, 1), 98.76m) };
        var msciWorld = new[] { new IndexedPoint(new DateOnly(2026, 1, 1), 100m), new IndexedPoint(new DateOnly(2026, 2, 1), 105.05m) };

        var cut = Render<HistoryChart>(p => p
            .Add(c => c.ROIC_Series, roic)
            .Add(c => c.LifeStrategySeries, lifeStrategy)
            .Add(c => c.MsciWorldSeries, msciWorld));

        Assert.Equal("Portefeuille (ROIC) — 112.4", HistoryChart.FormatSeriesName("Portefeuille (ROIC)", roic));
        Assert.Equal("LifeStrategy 40 — 98.8", HistoryChart.FormatSeriesName("LifeStrategy 40", lifeStrategy));
        Assert.Equal("MSCI World — 105.1", HistoryChart.FormatSeriesName("MSCI World", msciWorld));
    }

    [Fact]
    public void HistoryChart_WhenSeriesIsEmpty_ReturnsLabelUnchanged()
    {
        Assert.Equal("Portefeuille (ROIC)", HistoryChart.FormatSeriesName("Portefeuille (ROIC)", []));
    }
}

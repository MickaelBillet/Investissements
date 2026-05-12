using Bunit;
using InvestissementsDashboard.Client.Shared;
using InvestissementsDashboard.Client.Tests.Helpers;
using Microsoft.Extensions.DependencyInjection;
using MudBlazor.Services;

namespace InvestissementsDashboard.Client.Tests.Components;

public class KpiHeaderTests : BunitContext
{
    public KpiHeaderTests()
    {
        Services.AddMudServices(opt => opt.PopoverOptions.CheckForPopoverProvider = false);
        JSInterop.Mode = JSRuntimeMode.Loose;
    }

    [Fact]
    public void KpiHeader_WhenSnapshotIsNull_DisplaysDashForAmount()
    {
        var cut = Render<KpiHeader>(p => p
            .Add(c => c.Snapshot,   null)
            .Add(c => c.AssetCount, 0));

        Assert.Contains("—", cut.Markup);
    }

    [Fact]
    public void KpiHeader_WhenSnapshotProvided_DisplaysEuroSign()
    {
        var cut = Render<KpiHeader>(p => p
            .Add(c => c.Snapshot,   TestData.Snapshot(portfolio: 12_345m))
            .Add(c => c.AssetCount, 5));

        Assert.Contains("€", cut.Markup);
    }

    [Fact]
    public void KpiHeader_WhenSnapshotProvided_DisplaysFormattedDate()
    {
        var date = new DateOnly(2025, 6, 15);

        var cut = Render<KpiHeader>(p => p
            .Add(c => c.Snapshot,   TestData.Snapshot(date: date))
            .Add(c => c.AssetCount, 0));

        Assert.Contains("15/06/2025", cut.Markup);
    }

    [Fact]
    public void KpiHeader_DisplaysAssetCount()
    {
        var cut = Render<KpiHeader>(p => p
            .Add(c => c.Snapshot,   null)
            .Add(c => c.AssetCount, 12));

        Assert.Contains("12", cut.Markup);
    }
}

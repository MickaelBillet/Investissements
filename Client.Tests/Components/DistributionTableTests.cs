using Bunit;
using InvestissementsDashboard.Client.Model;
using InvestissementsDashboard.Client.Shared;
using Microsoft.Extensions.DependencyInjection;
using MudBlazor.Services;

namespace InvestissementsDashboard.Client.Tests.Components;

public class DistributionTableTests : BunitContext
{
    public DistributionTableTests()
    {
        Services.AddMudServices(opt => opt.PopoverOptions.CheckForPopoverProvider = false);
        JSInterop.Mode = JSRuntimeMode.Loose;
    }

    [Fact]
    public void DistributionTable_WhenItemsIsEmpty_DisplaysNoDataMessage()
    {
        var cut = Render<DistributionTable>(p => p
            .Add(c => c.Items, []));

        Assert.Contains("Aucune donnée", cut.Markup);
    }

    [Fact]
    public void DistributionTable_WhenItemsProvided_DisplaysItemNames()
    {
        var items = new[]
        {
            new DistributionItem("ETF_Stocks", 10_000m, 60m),
            new DistributionItem("Bonds",       6_000m,  40m)
        };

        var cut = Render<DistributionTable>(p => p
            .Add(c => c.Items, items));

        Assert.Contains("ETF_Stocks", cut.Markup);
        Assert.Contains("Bonds",      cut.Markup);
    }

    [Fact]
    public void DistributionTable_DisplaysFormattedCurrentTotal()
    {
        var items = new[] { new DistributionItem("Stocks", 12_345.67m, 100m) };

        var cut = Render<DistributionTable>(p => p
            .Add(c => c.Items, items));

        Assert.Contains("12", cut.Markup);
        Assert.Contains("€",  cut.Markup);
    }

    [Fact]
    public void DistributionTable_DisplaysFormattedWeight()
    {
        var items = new[] { new DistributionItem("Stocks", 10_000m, 75.5m) };

        var cut = Render<DistributionTable>(p => p
            .Add(c => c.Items, items));

        Assert.Contains("75", cut.Markup);
        Assert.Contains("%",  cut.Markup);
    }

    [Fact]
    public void DistributionTable_FooterDisplaysSumOfCurrentTotal()
    {
        var items = new[]
        {
            new DistributionItem("Stocks", 10_000m, 60m),
            new DistributionItem("Bonds",   6_000m,  40m)
        };

        var cut = Render<DistributionTable>(p => p
            .Add(c => c.Items, items));

        // Total = 16 000 → formatted as "16 000,00" or similar
        Assert.Contains("16", cut.Markup);
        Assert.Contains("Total", cut.Markup);
    }
}

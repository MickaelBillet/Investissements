using Bunit;
using InvestissementsDashboard.Client.Shared;
using InvestissementsDashboard.Client.Tests.Helpers;
using InvestissementsDashboard.Shared.Models;
using Microsoft.Extensions.DependencyInjection;
using MudBlazor.Services;

namespace InvestissementsDashboard.Client.Tests.Components;

public class BondScheduleChartTests : BunitContext
{
    public BondScheduleChartTests()
    {
        Services.AddMudServices(opt => opt.PopoverOptions.CheckForPopoverProvider = false);
        Services.AddLocalizationMock();
        JSInterop.Mode = JSRuntimeMode.Loose;
    }

    [Fact]
    public void BondScheduleChart_WhenItemsIsEmpty_DisplaysNoDataMessage()
    {
        var cut = Render<BondScheduleChart>(p => p
            .Add(c => c.Items, []));

        Assert.Contains("Aucune donnée", cut.Markup);
    }

    [Fact]
    public void BondScheduleChart_WhenItemsProvided_DoesNotShowNoDataMessage()
    {
        var items = new[] { new BondScheduleDto(2027, 1000m, []) };

        var cut = Render<BondScheduleChart>(p => p
            .Add(c => c.Items, items));

        Assert.DoesNotContain("Aucune donnée", cut.Markup);
    }
}

using Bunit;
using InvestissementsDashboard.Client.Shared;
using InvestissementsDashboard.Client.Tests.Helpers;
using InvestissementsDashboard.Shared.Models;
using Microsoft.Extensions.DependencyInjection;
using MudBlazor.Services;

namespace InvestissementsDashboard.Client.Tests.Components;

public class BondScheduleDetailTableTests : BunitContext
{
    public BondScheduleDetailTableTests()
    {
        Services.AddMudServices(opt => opt.PopoverOptions.CheckForPopoverProvider = false);
        Services.AddLocalizationMock();
        JSInterop.Mode = JSRuntimeMode.Loose;
    }

    [Fact]
    public void BondScheduleDetailTable_WhenBondsIsEmpty_DisplaysNoDataMessage()
    {
        var cut = Render<BondScheduleDetailTable>(p => p
            .Add(c => c.Bonds, [])
            .Add(c => c.Total, 0m));

        Assert.Contains("Aucune donnée", cut.Markup);
    }

    [Fact]
    public void BondScheduleDetailTable_WhenBondsProvided_DisplaysBondNames()
    {
        var bonds = new[]
        {
            new BondScheduleItemDto("Renault 2027", 1000m),
            new BondScheduleItemDto("Orange 2027",   500m)
        };

        var cut = Render<BondScheduleDetailTable>(p => p
            .Add(c => c.Bonds, bonds)
            .Add(c => c.Total, 1500m));

        Assert.Contains("Renault 2027", cut.Markup);
        Assert.Contains("Orange 2027",  cut.Markup);
    }

    [Fact]
    public void BondScheduleDetailTable_FooterDisplaysTotal()
    {
        var bonds = new[] { new BondScheduleItemDto("Renault 2027", 1000m) };

        var cut = Render<BondScheduleDetailTable>(p => p
            .Add(c => c.Bonds, bonds)
            .Add(c => c.Total, 1000m));

        Assert.Contains("Total", cut.Markup);
        Assert.Contains("1", cut.Markup);
        Assert.Contains("€", cut.Markup);
    }
}

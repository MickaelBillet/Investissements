using Bunit;
using InvestissementsDashboard.Client.Extensions;
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
        Services.AddPrivacyModeMock();
        JSInterop.Mode = JSRuntimeMode.Loose;
    }

    [Fact]
    public void BondScheduleDetailTable_WhenBondsIsEmpty_DisplaysNoDataMessage()
    {
        var cut = Render<BondScheduleDetailTable>(p => p
            .Add(c => c.PeriodLabel, "2027")
            .Add(c => c.Bonds, [])
            .Add(c => c.CapitalTotal, 0m)
            .Add(c => c.CouponsTotal, 0m)
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
            .Add(c => c.PeriodLabel, "2027")
            .Add(c => c.Bonds, bonds)
            .Add(c => c.CapitalTotal, 1500m)
            .Add(c => c.CouponsTotal, 0m)
            .Add(c => c.Total, 1500m));

        Assert.Contains("Renault 2027", cut.Markup);
        Assert.Contains("Orange 2027",  cut.Markup);
    }

    [Fact]
    public void BondScheduleDetailTable_FooterDisplaysTotal()
    {
        var bonds = new[] { new BondScheduleItemDto("Renault 2027", 1000m) };

        var cut = Render<BondScheduleDetailTable>(p => p
            .Add(c => c.PeriodLabel, "2027")
            .Add(c => c.Bonds, bonds)
            .Add(c => c.CapitalTotal, 1000m)
            .Add(c => c.CouponsTotal, 0m)
            .Add(c => c.Total, 1000m));

        Assert.Contains("Total", cut.Markup);
        Assert.Contains("1", cut.Markup);
        Assert.Contains("€", cut.Markup);
    }

    [Fact]
    public void BondScheduleDetailTable_DisplaysCapitalCouponsAndTotalColumns()
    {
        var bonds = new[] { new BondScheduleItemDto("Renault 2027", 1000m, 85m) };

        var cut = Render<BondScheduleDetailTable>(p => p
            .Add(c => c.PeriodLabel, "2027")
            .Add(c => c.Bonds, bonds)
            .Add(c => c.CapitalTotal, 1000m)
            .Add(c => c.CouponsTotal, 85m)
            .Add(c => c.Total, 1085m));

        var headers = cut.FindAll("th").Select(th => th.TextContent.Trim()).ToList();
        Assert.Equal(["Nom", "Capital", "Coupons", "Total"], headers);
    }

    [Fact]
    public void BondScheduleDetailTable_RowShowsCapitalCouponAndTheirSum()
    {
        var bonds = new[] { new BondScheduleItemDto("Renault 2027", 1000m, 85m) };

        var cut = Render<BondScheduleDetailTable>(p => p
            .Add(c => c.PeriodLabel, "2027")
            .Add(c => c.Bonds, bonds)
            .Add(c => c.CapitalTotal, 1000m)
            .Add(c => c.CouponsTotal, 85m)
            .Add(c => c.Total, 1085m));

        var cells = cut.Find("tbody tr").QuerySelectorAll("td").Select(td => td.TextContent.Trim()).ToList();
        Assert.Equal(1000m.ToEurAmount(), cells[1]);
        Assert.Equal(85m.ToEurAmount(), cells[2]);
        Assert.Equal(1085m.ToEurAmount(), cells[3]);
    }

    [Fact]
    public void BondScheduleDetailTable_FooterDisplaysProvidedTotals()
    {
        var bonds = new[] { new BondScheduleItemDto("Renault 2027", 1000m, 85m) };

        var cut = Render<BondScheduleDetailTable>(p => p
            .Add(c => c.PeriodLabel, "2027")
            .Add(c => c.Bonds, bonds)
            .Add(c => c.CapitalTotal, 1200m)
            .Add(c => c.CouponsTotal, 90m)
            .Add(c => c.Total, 1290m));

        var footer = cut.Find("tfoot").TextContent;
        Assert.Contains(1200m.ToEurAmount(), footer);
        Assert.Contains(90m.ToEurAmount(), footer);
        Assert.Contains(1290m.ToEurAmount(), footer);
    }

    [Fact]
    public void BondScheduleDetailTable_DisplaysPeriodLabelInHeader()
    {
        var bonds = new[] { new BondScheduleItemDto("Renault 2027", 1000m) };

        var cut = Render<BondScheduleDetailTable>(p => p
            .Add(c => c.PeriodLabel, "2027")
            .Add(c => c.Bonds, bonds)
            .Add(c => c.CapitalTotal, 1000m)
            .Add(c => c.CouponsTotal, 0m)
            .Add(c => c.Total, 1000m));

        Assert.Contains("2027", cut.Markup);
    }

    [Fact]
    public void BondScheduleDetailTable_DisplaysQuarterlyPeriodLabelInHeader()
    {
        var bonds = new[] { new BondScheduleItemDto("Renault 2027", 1000m) };

        var cut = Render<BondScheduleDetailTable>(p => p
            .Add(c => c.PeriodLabel, "T2 2027")
            .Add(c => c.Bonds, bonds)
            .Add(c => c.CapitalTotal, 1000m)
            .Add(c => c.CouponsTotal, 0m)
            .Add(c => c.Total, 1000m));

        Assert.Contains("T2 2027", cut.Markup);
    }

    [Fact]
    public void BondScheduleDetailTable_WhenPrivacyModeIsHidden_MasksAmounts()
    {
        Services.AddPrivacyModeMock(isHidden: true);
        var bonds = new[] { new BondScheduleItemDto("Renault 2027", 1000m) };

        var cut = Render<BondScheduleDetailTable>(p => p
            .Add(c => c.PeriodLabel, "2027")
            .Add(c => c.Bonds, bonds)
            .Add(c => c.CapitalTotal, 1000m)
            .Add(c => c.CouponsTotal, 0m)
            .Add(c => c.Total, 1000m));

        Assert.Contains("*****", cut.Markup);
        Assert.DoesNotContain("1 000", cut.Markup);
    }
}

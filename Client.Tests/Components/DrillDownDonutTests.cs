using Bunit;
using InvestissementsDashboard.Client.Model;
using InvestissementsDashboard.Client.Shared;
using InvestissementsDashboard.Client.Tests.Helpers;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using MudBlazor.Services;

namespace InvestissementsDashboard.Client.Tests.Components;

public class DrillDownDonutTests : BunitContext
{
    public DrillDownDonutTests()
    {
        Services.AddMudServices(opt => opt.PopoverOptions.CheckForPopoverProvider = false);
        Services.AddLocalizationMock();
        JSInterop.Mode = JSRuntimeMode.Loose;
    }

    [Fact]
    public void DrillDownDonut_WhenCanGoBackFalse_BackButtonIsAbsent()
    {
        var cut = Render<DrillDownDonut>(p => p
            .Add(c => c.Title,     "Classes d'actifs")
            .Add(c => c.Items,     [])
            .Add(c => c.CanGoBack, false));

        var buttons = cut.FindAll("button");
        Assert.Empty(buttons);
    }

    [Fact]
    public void DrillDownDonut_WhenCanGoBackTrue_BackButtonIsPresent()
    {
        var cut = Render<DrillDownDonut>(p => p
            .Add(c => c.Title,     "ETF Stocks")
            .Add(c => c.Items,     [])
            .Add(c => c.CanGoBack, true));

        var buttons = cut.FindAll("button");
        Assert.NotEmpty(buttons);
    }

    [Fact]
    public void DrillDownDonut_WhenBackButtonClicked_InvokesOnBackClicked()
    {
        var invoked = false;

        var cut = Render<DrillDownDonut>(p => p
            .Add(c => c.Title,         "ETF Stocks")
            .Add(c => c.Items,         [])
            .Add(c => c.CanGoBack,     true)
            .Add(c => c.OnBackClicked, EventCallback.Factory.Create(this, () => invoked = true)));

        cut.Find("button").Click();

        Assert.True(invoked);
    }

    [Fact]
    public void DrillDownDonut_WhenItemsIsEmpty_DisplaysNoDataMessage()
    {
        var cut = Render<DrillDownDonut>(p => p
            .Add(c => c.Title, "Classes d'actifs")
            .Add(c => c.Items, []));

        Assert.Contains("Aucune donnée", cut.Markup);
    }

    [Fact]
    public void DrillDownDonut_WhenItemsProvided_DoesNotShowNoDataMessage()
    {
        var items = new[]
        {
            new DistributionItem("Stocks", "Actions",    10_000m, 60m),
            new DistributionItem("Bonds",  "Obligations", 6_000m, 40m)
        };

        var cut = Render<DrillDownDonut>(p => p
            .Add(c => c.Title, "Classes d'actifs")
            .Add(c => c.Items, items));

        Assert.DoesNotContain("Aucune donnée", cut.Markup);
    }

    [Fact]
    public void DrillDownDonut_DisplaysTitle()
    {
        var cut = Render<DrillDownDonut>(p => p
            .Add(c => c.Title, "Mon titre")
            .Add(c => c.Items, []));

        Assert.Contains("Mon titre", cut.Markup);
    }

    [Fact]
    public void DrillDownDonut_WhenTopRightContentProvided_RendersItInHeader()
    {
        var cut = Render<DrillDownDonut>(p => p
            .Add(c => c.Title,            "ETF Stocks")
            .Add(c => c.Items,            [])
            .Add(c => c.TopRightContent,  b => b.AddMarkupContent(0, "<span id='top-right'>Mon contenu</span>")));

        Assert.Contains("Mon contenu", cut.Markup);
    }

    [Fact]
    public void DrillDownDonut_WhenTopRightContentIsNull_NoExtraContentInHeader()
    {
        var cut = Render<DrillDownDonut>(p => p
            .Add(c => c.Title, "ETF Stocks")
            .Add(c => c.Items, []));

        // Default render should not throw and title must be present
        Assert.Contains("ETF Stocks", cut.Markup);
    }
}

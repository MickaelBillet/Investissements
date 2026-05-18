using Bunit;
using InvestissementsDashboard.Client.Shared;
using InvestissementsDashboard.Client.Tests.Helpers;
using InvestissementsDashboard.Shared.Models;
using Microsoft.Extensions.DependencyInjection;
using MudBlazor.Services;

namespace InvestissementsDashboard.Client.Tests.Components;

public class AssetTableTests : BunitContext
{
    public AssetTableTests()
    {
        Services.AddMudServices(opt => opt.PopoverOptions.CheckForPopoverProvider = false);
        JSInterop.Mode = JSRuntimeMode.Loose;
    }

    [Fact]
    public void AssetTable_WhenNoAssets_DisplaysNoRecordsMessage()
    {
        var cut = Render<AssetTable>(p => p.Add(c => c.Assets, []));

        Assert.Contains("Aucun actif", cut.Markup);
    }

    [Fact]
    public void AssetTable_WhenAssetsProvided_DisplaysAssetNames()
    {
        var assets = new[]
        {
            TestData.Asset(name: "MSCI World", currentTotal: 5_000m),
            TestData.Asset(name: "Livret A",   currentTotal: 3_000m)
        };

        var cut = Render<AssetTable>(p => p.Add(c => c.Assets, assets));

        Assert.Contains("MSCI World", cut.Markup);
        Assert.Contains("Livret A",   cut.Markup);
    }

    [Fact]
    public void AssetTable_WhenRoiIsPositive_AppliesPositiveCssClass()
    {
        var assets = new[] { TestData.Asset(roi: 15.5m, currentTotal: 1_000m) };

        var cut = Render<AssetTable>(p => p.Add(c => c.Assets, assets));

        Assert.Contains("roi-positive", cut.Markup);
    }

    [Fact]
    public void AssetTable_WhenRoiIsNegative_AppliesNegativeCssClass()
    {
        var assets = new[] { TestData.Asset(roi: -5m, currentTotal: 1_000m) };

        var cut = Render<AssetTable>(p => p.Add(c => c.Assets, assets));

        Assert.Contains("roi-negative", cut.Markup);
    }

    [Fact]
    public void AssetTable_WhenCurrentTotalIsNull_DisplaysDash()
    {
        var nullAsset = new AssetDto(
            1, "NullAsset", "Stocks", "PEA", "PEA TR", "ETF_Stocks", "", "", 3,
            null, null, null, null, null, null, null, 0m);

        var cut = Render<AssetTable>(p => p.Add(c => c.Assets, [nullAsset]));

        Assert.Contains("—", cut.Markup);
    }

    [Fact]
    public void AssetTable_FooterDisplaysSumOfCurrentTotal()
    {
        var assets = new[]
        {
            TestData.Asset(name: "A", currentTotal: 5_000m),
            TestData.Asset(name: "B", currentTotal: 3_000m)
        };

        var cut = Render<AssetTable>(p => p.Add(c => c.Assets, assets));

        Assert.Contains("Total", cut.Markup);
        Assert.Contains("8",     cut.Markup); // 8 000
        Assert.Contains("€",     cut.Markup);
    }
}

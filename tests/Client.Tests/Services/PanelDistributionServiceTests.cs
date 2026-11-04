using InvestissementsDashboard.Client.Model;
using InvestissementsDashboard.Client.Services;
using InvestissementsDashboard.Client.Tests.Helpers;
using InvestissementsDashboard.Shared.Models;
using Moq;

namespace InvestissementsDashboard.Client.Tests.Services;

public class PanelDistributionServiceTests
{
    private static readonly IReadOnlyList<AssetTypeReferenceDto> _types = TestData.DefaultAssetTypeReference;

    private static PanelDistributionService CreateService()
    {
        var loc = new Mock<ILocalizationService>();
        loc.Setup(l => l.Translate(It.IsAny<string>())).Returns((string k) => $"tr:{k}");
        return new PanelDistributionService(loc.Object);
    }

    private static PanelState Panel(PanelType type, params string[] path)
    {
        var panel = new PanelState(type);
        foreach (var name in path) panel.DrillDown(name);
        return panel;
    }

    private static readonly AssetDto[] _etfs =
    [
        TestData.Asset(id: 1, name: "MSCI World", assetType: "ETF_Stocks", information: "World",  currentTotal: 3_000m),
        TestData.Asset(id: 2, name: "S&P 500",    assetType: "ETF_Stocks", information: "World",  currentTotal: 2_000m),
        TestData.Asset(id: 3, name: "Stoxx 600",  assetType: "ETF_Stocks", information: "Europe", currentTotal: 1_000m),
        TestData.Asset(id: 4, name: "Air Liquide", assetType: "Stock",                            currentTotal: 500m),
    ];

    // ── GetDistribution : AssetClass ──────────────────────────────────────────

    [Fact]
    public void GetDistribution_WhenAssetClassLevel0_GroupsByTranslatedAssetClass()
    {
        var assets = new[] { TestData.Asset(id: 1, assetClass: "Stocks"), TestData.Asset(id: 2, assetClass: "Bonds") };

        var result = CreateService().GetDistribution(assets, _types, Panel(PanelType.AssetClass), false);

        Assert.Equal(2, result.Count);
        Assert.Equal("tr:Stocks", result.Single(i => i.Name == "Stocks").Label);
    }

    [Fact]
    public void GetDistribution_WhenAssetClassLevel1_GroupsByTypeWithSheetLabel()
    {
        var assets = new[]
        {
            TestData.Asset(id: 1, assetClass: "Stocks", assetType: "ETF_Stocks"),
            TestData.Asset(id: 2, assetClass: "Bonds",  assetType: "MarketBonds"),
        };

        var result = CreateService().GetDistribution(assets, _types, Panel(PanelType.AssetClass, "Stocks"), false);

        var item = Assert.Single(result);
        Assert.Equal("ETF_Stocks", item.Name);
        Assert.Equal("ETF Actions", item.Label);
    }

    [Fact]
    public void GetDistribution_WhenEtfLevel2AndGrouped_GroupsByInformation()
    {
        var result = CreateService().GetDistribution(_etfs, _types, Panel(PanelType.AssetClass, "Stocks", "ETF_Stocks"), true);

        Assert.Equal(["World", "Europe"], result.Select(i => i.Name));
    }

    [Fact]
    public void GetDistribution_WhenEtfLevel2AndNotGrouped_GroupsByName()
    {
        var result = CreateService().GetDistribution(_etfs, _types, Panel(PanelType.AssetClass, "Stocks", "ETF_Stocks"), false);

        Assert.Equal(["MSCI World", "S&P 500", "Stoxx 600"], result.Select(i => i.Name));
    }

    [Fact]
    public void GetDistribution_WhenEtfLevel3_GroupsAssetsOfTheInformationGroupByName()
    {
        var panel = Panel(PanelType.AssetClass, "Stocks", "ETF_Stocks", "World");

        var result = CreateService().GetDistribution(_etfs, _types, panel, true);

        Assert.Equal(["MSCI World", "S&P 500"], result.Select(i => i.Name));
    }

    // ── GetDistribution : SupportType / Risk ──────────────────────────────────

    [Fact]
    public void GetDistribution_WhenSupportTypeLevel1_GroupsBySupport()
    {
        var assets = new[]
        {
            TestData.Asset(id: 1, supportType: "PEA", support: "PEA TR"),
            TestData.Asset(id: 2, supportType: "CTO", support: "CTO TR"),
        };

        var result = CreateService().GetDistribution(assets, _types, Panel(PanelType.SupportType, "PEA"), false);

        Assert.Equal("PEA TR", Assert.Single(result).Name);
    }

    [Fact]
    public void GetDistribution_WhenRiskLevel0_UsesRiskTranslationKey()
    {
        var assets = new[] { TestData.Asset(id: 1, risk: 2) };

        var item = Assert.Single(CreateService().GetDistribution(assets, _types, Panel(PanelType.Risk), false));

        Assert.Equal("2", item.Name);
        Assert.Equal("tr:Risk_2", item.Label);
    }

    // ── GetLeafAssets ─────────────────────────────────────────────────────────

    [Fact]
    public void GetLeafAssets_WhenNotAtLeafLevel_ReturnsEmpty()
    {
        Assert.Empty(CreateService().GetLeafAssets(_etfs, Panel(PanelType.AssetClass, "Stocks"), false));
    }

    [Fact]
    public void GetLeafAssets_WhenAssetClassLeaf_ReturnsMatchingAssetsSortedByValue()
    {
        var result = CreateService().GetLeafAssets(_etfs, Panel(PanelType.AssetClass, "Stocks", "ETF_Stocks"), false);

        Assert.Equal(["MSCI World", "S&P 500", "Stoxx 600"], result.Select(a => a.Name));
    }

    [Fact]
    public void GetLeafAssets_WhenEtfGroupedAtLevel3_ReturnsOnlyTheInformationGroup()
    {
        var panel = Panel(PanelType.AssetClass, "Stocks", "ETF_Stocks", "Europe");

        var result = CreateService().GetLeafAssets(_etfs, panel, true);

        Assert.Equal("Stoxx 600", Assert.Single(result).Name);
    }

    [Fact]
    public void GetLeafAssets_WhenSupportTypeLeaf_FiltersBySupport()
    {
        var assets = new[]
        {
            TestData.Asset(id: 1, supportType: "PEA", support: "PEA TR"),
            TestData.Asset(id: 2, supportType: "CTO", support: "CTO TR"),
        };

        var result = CreateService().GetLeafAssets(assets, Panel(PanelType.SupportType, "CTO", "CTO TR"), false);

        Assert.Equal(2, Assert.Single(result).Id);
    }

    [Fact]
    public void GetLeafAssets_WhenRiskLeaf_FiltersByRisk()
    {
        var assets = new[] { TestData.Asset(id: 1, risk: 1), TestData.Asset(id: 2, risk: 4) };

        var result = CreateService().GetLeafAssets(assets, Panel(PanelType.Risk, "4"), false);

        Assert.Equal(2, Assert.Single(result).Id);
    }

    // ── TranslateAssetType ────────────────────────────────────────────────────

    [Fact]
    public void TranslateAssetType_WhenKnown_ReturnsSheetLabel()
    {
        Assert.Equal("Obligations cotées", CreateService().TranslateAssetType(_types, "MarketBonds"));
    }

    [Fact]
    public void TranslateAssetType_WhenUnknown_ReturnsRawName()
    {
        Assert.Equal("Crypto", CreateService().TranslateAssetType(_types, "Crypto"));
    }
}

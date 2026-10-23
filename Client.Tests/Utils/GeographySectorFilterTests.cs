using InvestissementsDashboard.Client.Tests.Helpers;
using InvestissementsDashboard.Client.Utils;
using InvestissementsDashboard.Shared.Models;

namespace InvestissementsDashboard.Client.Tests.Utils;

public class GeographySectorFilterTests
{
    private static readonly IReadOnlyList<AssetTypeReferenceDto> _types = TestData.DefaultAssetTypeReference;

    // ── AssetsForZone ─────────────────────────────────────────────────────────

    [Fact]
    public void AssetsForZone_MatchesExactZoneNotSubstring()
    {
        var assets = new[]
        {
            TestData.Asset(id: 1, name: "India Fund",   geography: "Inde : 100%"),
            TestData.Asset(id: 2, name: "Indexed Fund", geography: "Indépendance : 100%"),
        };

        var result = GeographySectorFilter.AssetsForZone(assets, _types, "Stocks", "Inde");

        Assert.Equal("India Fund", Assert.Single(result).Name);
    }

    [Fact]
    public void AssetsForZone_ExcludesOtherAssetClass()
    {
        var assets = new[]
        {
            TestData.Asset(id: 1, assetClass: "Stocks", assetType: "Stock",       geography: "Europe : 100%"),
            TestData.Asset(id: 2, assetClass: "Bonds",  assetType: "MarketBonds", geography: "Europe : 100%"),
        };

        var result = GeographySectorFilter.AssetsForZone(assets, _types, "Bonds", "Europe");

        Assert.Equal(2, Assert.Single(result).Id);
    }

    [Fact]
    public void AssetsForZone_ExcludesTypeNotGeoSectorEligible()
    {
        var assets = new[]
        {
            TestData.Asset(id: 1, assetClass: "Bonds", assetType: "MarketBonds", geography: "Europe : 100%"),
            TestData.Asset(id: 2, assetClass: "Bonds", assetType: "ETF_Bunds",   geography: "Europe : 100%"),
        };

        var result = GeographySectorFilter.AssetsForZone(assets, _types, "Bonds", "Europe");

        Assert.Equal(1, Assert.Single(result).Id);
    }

    [Fact]
    public void AssetsForZone_IsSortedByCurrentTotalDescending()
    {
        var assets = new[]
        {
            TestData.Asset(id: 1, name: "Small", geography: "USA : 100%",            currentTotal: 1_000m),
            TestData.Asset(id: 2, name: "Large", geography: "USA : 60% - Asie : 40%", currentTotal: 5_000m),
        };

        var result = GeographySectorFilter.AssetsForZone(assets, _types, "Stocks", "USA");

        Assert.Equal(["Large", "Small"], result.Select(a => a.Name));
    }

    // ── ZoneCoefficients ──────────────────────────────────────────────────────

    [Fact]
    public void ZoneCoefficients_ReturnsPctOfTheZoneForEachMatchingAsset()
    {
        var assets = new[]
        {
            TestData.Asset(id: 1, geography: "USA : 41% - Europe : 24%"),
            TestData.Asset(id: 2, geography: "USA : 100%"),
        };

        var result = GeographySectorFilter.ZoneCoefficients(assets, _types, "Stocks", "USA");

        Assert.Equal(0.41m, result[1]);
        Assert.Equal(1m,    result[2]);
    }

    [Fact]
    public void ZoneCoefficients_WhenNoAssetInZone_ReturnsEmpty()
    {
        var assets = new[] { TestData.Asset(id: 1, geography: "USA : 100%") };

        Assert.Empty(GeographySectorFilter.ZoneCoefficients(assets, _types, "Stocks", "Europe"));
    }

    // ── AssetsForSector ───────────────────────────────────────────────────────

    [Fact]
    public void AssetsForSector_FiltersByClassAndSector()
    {
        var assets = new[]
        {
            TestData.Asset(id: 1, name: "A", assetClass: "Stocks", sector: "Technology"),
            TestData.Asset(id: 2, name: "B", assetClass: "Stocks", sector: "Finance"),
            TestData.Asset(id: 3, name: "C", assetClass: "Bonds",  assetType: "MarketBonds", sector: "Technology"),
        };

        var result = GeographySectorFilter.AssetsForSector(assets, _types, "Stocks", "Technology");

        Assert.Equal("A", Assert.Single(result).Name);
    }

    [Fact]
    public void AssetsForSector_ExcludesTypeNotGeoSectorEligible()
    {
        var assets = new[]
        {
            TestData.Asset(id: 1, assetClass: "Bonds", assetType: "MarketBonds", sector: "Sovereign"),
            TestData.Asset(id: 2, assetClass: "Bonds", assetType: "ETF_Bunds",   sector: "Sovereign"),
        };

        var result = GeographySectorFilter.AssetsForSector(assets, _types, "Bonds", "Sovereign");

        Assert.Equal(1, Assert.Single(result).Id);
    }

    [Fact]
    public void AssetsForSector_IsSortedByCurrentTotalDescending()
    {
        var assets = new[]
        {
            TestData.Asset(id: 1, name: "Small", sector: "Technology", currentTotal: 1_000m),
            TestData.Asset(id: 2, name: "Large", sector: "Technology", currentTotal: 5_000m),
        };

        var result = GeographySectorFilter.AssetsForSector(assets, _types, "Stocks", "Technology");

        Assert.Equal(["Large", "Small"], result.Select(a => a.Name));
    }
}

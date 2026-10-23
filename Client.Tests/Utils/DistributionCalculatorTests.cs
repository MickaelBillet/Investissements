using InvestissementsDashboard.Client.Tests.Helpers;
using InvestissementsDashboard.Client.Utils;
using InvestissementsDashboard.Shared.Models;

namespace InvestissementsDashboard.Client.Tests.Utils;

public class DistributionCalculatorTests
{
    [Fact]
    public void Compute_GroupsByKeyAndSumsCurrentTotal()
    {
        var assets = new[]
        {
            TestData.Asset(id: 1, assetClass: "Stocks", currentTotal: 600m),
            TestData.Asset(id: 2, assetClass: "Stocks", currentTotal: 400m),
            TestData.Asset(id: 3, assetClass: "Bonds",  currentTotal: 500m),
        };

        var result = DistributionCalculator.Compute(assets, a => a.AssetClass);

        Assert.Equal(2, result.Count);
        Assert.Equal(1_000m, result.Single(i => i.Name == "Stocks").CurrentTotal);
        Assert.Equal(500m,   result.Single(i => i.Name == "Bonds").CurrentTotal);
    }

    [Fact]
    public void Compute_WeightsSumTo100()
    {
        var assets = new[]
        {
            TestData.Asset(id: 1, assetClass: "Stocks", currentTotal: 300m),
            TestData.Asset(id: 2, assetClass: "Bonds",  currentTotal: 200m),
            TestData.Asset(id: 3, assetClass: "Cash",   currentTotal: 500m),
        };

        var result = DistributionCalculator.Compute(assets, a => a.AssetClass);

        Assert.Equal(100m, result.Sum(i => i.Weight));
        Assert.Equal(50m, result.Single(i => i.Name == "Cash").Weight);
    }

    [Fact]
    public void Compute_IsSortedByCurrentTotalDescending()
    {
        var assets = new[]
        {
            TestData.Asset(id: 1, assetClass: "Cash",   currentTotal: 100m),
            TestData.Asset(id: 2, assetClass: "Stocks", currentTotal: 900m),
            TestData.Asset(id: 3, assetClass: "Bonds",  currentTotal: 400m),
        };

        var result = DistributionCalculator.Compute(assets, a => a.AssetClass);

        Assert.Equal(["Stocks", "Bonds", "Cash"], result.Select(i => i.Name));
    }

    [Fact]
    public void Compute_WhenConvertKeyGiven_TranslatesLabelButKeepsName()
    {
        var assets = new[] { TestData.Asset(assetClass: "Stocks") };

        var item = Assert.Single(DistributionCalculator.Compute(assets, a => a.AssetClass, k => $"label:{k}"));

        Assert.Equal("Stocks", item.Name);
        Assert.Equal("label:Stocks", item.Label);
    }

    [Fact]
    public void Compute_WhenNoConvertKey_LabelIsTheName()
    {
        var item = Assert.Single(DistributionCalculator.Compute([TestData.Asset(assetClass: "Stocks")], a => a.AssetClass));

        Assert.Equal("Stocks", item.Label);
    }

    [Fact]
    public void Compute_WhenTotalIsZero_WeightsAreZero()
    {
        var item = Assert.Single(DistributionCalculator.Compute([TestData.Asset(currentTotal: 0m)], a => a.AssetClass));

        Assert.Equal(0m, item.Weight);
    }

    [Fact]
    public void Compute_WhenNoAssets_ReturnsEmpty()
    {
        Assert.Empty(DistributionCalculator.Compute([], a => a.AssetClass));
    }

    // ── FromApi ───────────────────────────────────────────────────────────────

    [Fact]
    public void FromApi_KeepsApiValuesAndOrder()
    {
        var distribution = new[]
        {
            new DistributionDto("Europe", 600m, 60m),
            new DistributionDto("USA",    400m, 40m),
        };

        var result = DistributionCalculator.FromApi(distribution);

        Assert.Equal(["Europe", "USA"], result.Select(i => i.Name));
        Assert.Equal(600m, result[0].CurrentTotal);
        Assert.Equal(60m,  result[0].Weight);
        Assert.Equal("Europe", result[0].Label);
    }

    [Fact]
    public void FromApi_WhenToLabelGiven_TranslatesLabelButKeepsName()
    {
        var item = Assert.Single(DistributionCalculator.FromApi([new DistributionDto("Technology", 1m, 100m)], k => $"tr:{k}"));

        Assert.Equal("Technology", item.Name);
        Assert.Equal("tr:Technology", item.Label);
    }
}

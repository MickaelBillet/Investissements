using InvestissementsDashboard.Client.Services;
using InvestissementsDashboard.Client.Tests.Helpers;
using InvestissementsDashboard.Client.ViewModels;
using Moq;

namespace InvestissementsDashboard.Client.Tests.ViewModels;

public class AssetViewModelTests
{
    private static AssetViewModel Create(bool withRunner = true) =>
        new(withRunner ? new Mock<IAgentRunner>().Object : null);

    [Fact]
    public void IsAgentAvailable_WhenNoRunnerRegistered_IsFalse()
    {
        Assert.False(Create(withRunner: false).IsAgentAvailable);
    }

    [Fact]
    public void IsAgentAvailable_WhenRunnerRegistered_IsTrue()
    {
        Assert.True(Create().IsAgentAvailable);
    }

    [Fact]
    public void CanLaunchAgent_WhenRunnerAndStockAsset_IsTrue()
    {
        Assert.True(Create().CanLaunchAgent(TestData.Asset(assetType: "Stock")));
    }

    [Fact]
    public void CanLaunchAgent_WhenRunnerAndNonStockAsset_IsFalse()
    {
        Assert.False(Create().CanLaunchAgent(TestData.Asset(assetType: "ETF_Stocks")));
    }

    [Fact]
    public void CanLaunchAgent_WhenNoRunnerAndStockAsset_IsFalse()
    {
        Assert.False(Create(withRunner: false).CanLaunchAgent(TestData.Asset(assetType: "Stock")));
    }

    [Fact]
    public void GetTotal_WithoutCoefficients_ReturnsRawSum()
    {
        var assets = new[] { TestData.Asset(currentTotal: 1_000m), TestData.Asset(currentTotal: 2_000m) };

        Assert.Equal(3_000m, Create().GetTotal(assets));
    }

    [Fact]
    public void GetTotal_WhenCurrentTotalIsNull_CountsItAsZero()
    {
        var assets = new[] { TestData.Asset(currentTotal: 1_000m), TestData.Asset(currentTotal: 0m) with { CurrentTotal = null } };

        Assert.Equal(1_000m, Create().GetTotal(assets));
    }

    [Fact]
    public void GetTotal_WithCoefficients_ReturnsWeightedSum()
    {
        var assets = new[]
        {
            TestData.Asset(id: 1, currentTotal: 1_000m), // 41% -> 410
            TestData.Asset(id: 2, currentTotal: 2_000m)  // 24% -> 480
        };
        var coefficients = new Dictionary<int, decimal> { [1] = 0.41m, [2] = 0.24m };

        Assert.Equal(890m, Create().GetTotal(assets, coefficients));
    }

    [Fact]
    public void GetTotal_WhenAssetHasNoCoefficient_ExcludesIt()
    {
        var assets = new[] { TestData.Asset(id: 1, currentTotal: 1_000m), TestData.Asset(id: 2, currentTotal: 2_000m) };
        var coefficients = new Dictionary<int, decimal> { [1] = 0.5m };

        Assert.Equal(500m, Create().GetTotal(assets, coefficients));
    }
}

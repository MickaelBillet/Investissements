using InvestissementsDashboard.Client.Model;

namespace InvestissementsDashboard.Client.Tests.Models;

public class PanelStateTests
{
    [Fact]
    public void DrillDown_WhenCalled_IncreasesLevelAndEnablesBack()
    {
        var panel = new PanelState(PanelType.AssetClass);

        panel.DrillDown("Stocks");

        Assert.Equal(1, panel.Level);
        Assert.True(panel.CanGoBack);
    }

    [Fact]
    public void GoBack_AfterDrillDown_DecreasesLevelToZero()
    {
        var panel = new PanelState(PanelType.AssetClass);
        panel.DrillDown("Stocks");

        panel.GoBack();

        Assert.Equal(0, panel.Level);
        Assert.False(panel.CanGoBack);
    }

    [Fact]
    public void GoBack_WhenAtRoot_DoesNotThrow()
    {
        var panel = new PanelState(PanelType.AssetClass);

        var ex = Record.Exception(() => panel.GoBack());

        Assert.Null(ex);
        Assert.Equal(0, panel.Level);
    }

    [Fact]
    public void IsAtLeafLevel_ForRisk_TrueAtLevel1()
    {
        var panel = new PanelState(PanelType.Risk);
        panel.DrillDown("3");

        Assert.True(panel.IsAtLeafLevel);
    }

    [Fact]
    public void IsAtLeafLevel_ForAssetClass_FalseAtLevel1()
    {
        var panel = new PanelState(PanelType.AssetClass);
        panel.DrillDown("Stocks");

        Assert.False(panel.IsAtLeafLevel);
    }

    [Fact]
    public void IsAtLeafLevel_ForAssetClass_TrueAtLevel2()
    {
        var panel = new PanelState(PanelType.AssetClass);
        panel.DrillDown("Stocks");
        panel.DrillDown("ETF_Stocks");

        Assert.True(panel.IsAtLeafLevel);
    }

    [Fact]
    public void Selected_WhenPathHasItems_ReturnsCorrectValueAtEachIndex()
    {
        var panel = new PanelState(PanelType.AssetClass);
        panel.DrillDown("Stocks");
        panel.DrillDown("ETF_Stocks");

        Assert.Equal("Stocks",     panel.Selected(0));
        Assert.Equal("ETF_Stocks", panel.Selected(1));
        Assert.Null(panel.Selected(2));
    }

    [Theory]
    [InlineData(PanelType.AssetClass,  "Classes d'actifs")]
    [InlineData(PanelType.SupportType, "Types de supports")]
    [InlineData(PanelType.Risk,        "Niveaux de risque")]
    public void BreadcrumbLabel_WhenAtRoot_ReturnsDefaultLabel(PanelType type, string expected)
    {
        var panel = new PanelState(type);

        Assert.Equal(expected, panel.BreadcrumbLabel);
    }

    [Fact]
    public void BreadcrumbLabel_AfterTwoDrillDowns_ReturnsJoinedPath()
    {
        var panel = new PanelState(PanelType.AssetClass);
        panel.DrillDown("Stocks");
        panel.DrillDown("ETF_Stocks");

        Assert.Equal("Stocks › ETF_Stocks", panel.BreadcrumbLabel);
    }
}

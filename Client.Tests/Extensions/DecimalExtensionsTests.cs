using InvestissementsDashboard.Client.Extensions;

namespace InvestissementsDashboard.Client.Tests.Extensions;

public class DecimalExtensionsTests
{
    [Fact]
    public void ToEurAmount_WhenNullDecimal_ReturnsDash()
    {
        decimal? value = null;

        Assert.Equal("—", value.ToEurAmount());
    }

    [Fact]
    public void ToEurAmount_WhenDecimal_ContainsEuroSign()
    {
        var result = (1_500m).ToEurAmount();

        Assert.Contains("€", result);
    }

    [Fact]
    public void ToEurAmount_WhenDecimal_ContainsNumericValue()
    {
        var result = (1_500m).ToEurAmount();

        Assert.Contains("1", result);
        Assert.Contains("500", result);
    }

    [Fact]
    public void ToPercentage_WhenNullDecimal_ReturnsDash()
    {
        decimal? value = null;

        Assert.Equal("—", value.ToPercentage());
    }

    [Fact]
    public void ToPercentage_WhenDecimal_EndsWithPercentSign()
    {
        var result = (42.5m).ToPercentage();

        Assert.EndsWith("%", result.TrimEnd());
    }

    [Fact]
    public void ToPercentage_WhenDecimal_ContainsNumericValue()
    {
        var result = (42.5m).ToPercentage();

        Assert.Contains("42", result);
    }

    [Fact]
    public void CssRoiClass_WhenPositive_ReturnsPositiveClass()
    {
        decimal? value = 100m;

        Assert.Equal("roi-positive", value.CssRoiClass());
    }

    [Fact]
    public void CssRoiClass_WhenNegative_ReturnsNegativeClass()
    {
        decimal? value = -50m;

        Assert.Equal("roi-negative", value.CssRoiClass());
    }

    [Fact]
    public void CssRoiClass_WhenNull_ReturnsEmptyString()
    {
        decimal? value = null;

        Assert.Equal("", value.CssRoiClass());
    }

    [Fact]
    public void CssRoiClass_WhenZero_ReturnsEmptyString()
    {
        decimal? value = 0m;

        Assert.Equal("", value.CssRoiClass());
    }

    [Fact]
    public void ToSignedPercentage_WhenPositive_StartWithPlusSign()
    {
        Assert.StartsWith("+", (1.23m).ToSignedPercentage());
    }

    [Fact]
    public void ToSignedPercentage_WhenNegative_DoesNotDoubleSign()
    {
        var result = (-1.23m).ToSignedPercentage();

        Assert.StartsWith("-", result);
        Assert.DoesNotContain("+-", result);
    }

    [Fact]
    public void ToSignedPercentage_WhenZero_StartWithPlusSign()
    {
        Assert.StartsWith("+", (0m).ToSignedPercentage());
    }
}

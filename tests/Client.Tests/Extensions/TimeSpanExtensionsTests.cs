using InvestissementsDashboard.Client.Extensions;

namespace InvestissementsDashboard.Client.Tests.Extensions;

public class TimeSpanExtensionsTests
{
    [Theory]
    [InlineData(0, "< 1 s")]
    [InlineData(0.4, "< 1 s")]
    [InlineData(1.5, "2 s")]
    [InlineData(59, "59 s")]
    [InlineData(59.6, "1 min 0 s")]
    [InlineData(78, "1 min 18 s")]
    public void ToShortDuration_AnyDuration_FormatsItInSecondsThenMinutes(double seconds, string expected)
    {
        Assert.Equal(expected, TimeSpan.FromSeconds(seconds).ToShortDuration());
    }
}

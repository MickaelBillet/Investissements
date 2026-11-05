namespace InvestissementsDashboard.Client.Extensions;

public static class TimeSpanExtensions
{
    public static string ToShortDuration(this TimeSpan value)
    {
        var seconds = (long)Math.Round(value.TotalSeconds, MidpointRounding.AwayFromZero);
        return seconds switch
        {
            < 1 => "< 1 s",
            < 60 => $"{seconds} s",
            _ => $"{seconds / 60} min {seconds % 60} s"
        };
    }
}

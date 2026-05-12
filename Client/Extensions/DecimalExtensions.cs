using System.Globalization;

namespace InvestissementsDashboard.Client.Extensions;

public static class DecimalExtensions
{
    public static string ToEurAmount(this decimal? value) =>
        value.HasValue ? value.Value.ToEurAmount() : "—";

    public static string ToEurAmount(this decimal value) =>
        $"€ {value.ToString("N2", CultureInfo.GetCultureInfo("fr-FR"))}";

    public static string ToPercentage(this decimal? value, int decimals = 2) =>
        value.HasValue ? value.Value.ToPercentage(decimals) : "—";

    public static string ToPercentage(this decimal value, int decimals = 2) =>
        $"{value.ToString($"N{decimals}", CultureInfo.InvariantCulture)} %";

    public static string CssRoiClass(this decimal? value) =>
        value switch { > 0 => "roi-positive", < 0 => "roi-negative", _ => "" };
}

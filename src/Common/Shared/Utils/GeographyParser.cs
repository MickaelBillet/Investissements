using System.Globalization;

namespace InvestissementsDashboard.Shared;

public static class GeographyParser
{
    // Parse "Zone1 : X% - Zone2 : Y%" → (zone, pct) pairs
    public static IEnumerable<(string Zone, decimal Pct)> Parse(string geography)
    {
        if (string.IsNullOrWhiteSpace(geography)) yield break;

        var parts = geography.Split('-', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);

        foreach (var part in parts)
        {
            var sepIdx = part.LastIndexOf(':');
            if (sepIdx == -1) continue;

            var zone   = part[..sepIdx].Trim();
            var pctStr = part[(sepIdx + 1)..].Replace("%", "").Trim();

            if (!string.IsNullOrEmpty(zone)
                && decimal.TryParse(pctStr, NumberStyles.Number, CultureInfo.InvariantCulture, out var pct))
                yield return (zone, pct / 100m);
        }
    }
}

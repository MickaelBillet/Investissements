using System.Text.RegularExpressions;
using InvestissementsDashboard.Shared.Models;

namespace InvestissementsDashboard.Api.Services;

internal sealed partial class BondScheduleService(IAssetsService assetsService) : IBondScheduleService
{
    public async Task<IReadOnlyList<BondScheduleDto>> GetScheduleAsync(CancellationToken ct = default)
    {
        var assets = await assetsService.GetAllAsync(ct);

        var yearMap = new Dictionary<int, decimal>();

        foreach (var asset in assets)
        {
            var year = ExtractYear(asset.Information);
            if (year is null) continue;

            var amount = asset.CurrentTotal;
            if (amount is null) continue;

            yearMap.TryAdd(year.Value, 0m);
            yearMap[year.Value] += amount.Value;
        }

        return yearMap
            .Select(kv => new BondScheduleDto(kv.Key, kv.Value))
            .OrderBy(d => d.Year)
            .ToArray();
    }

    // Extract a 4-digit year (2000-2099) isolated in the free-text Information field
    internal static int? ExtractYear(string information)
    {
        if (string.IsNullOrWhiteSpace(information)) return null;

        var match = YearRegex().Match(information);
        return match.Success ? int.Parse(match.Groups[1].Value) : null;
    }

    [GeneratedRegex(@"\b(20\d{2})\b")]
    private static partial Regex YearRegex();
}

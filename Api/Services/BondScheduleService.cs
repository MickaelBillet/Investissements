using System.Text.RegularExpressions;
using InvestissementsDashboard.Shared.Models;

namespace InvestissementsDashboard.Api.Services;

internal sealed partial class BondScheduleService(IAssetsService assetsService) : IBondScheduleService
{
    public async Task<IReadOnlyList<BondScheduleDto>> GetScheduleAsync(CancellationToken ct = default)
    {
        var assets = await assetsService.GetAllAsync(ct);

        var periodMap = new Dictionary<(int Year, int Month), List<BondScheduleItemDto>>();

        foreach (var asset in assets)
        {
            var maturity = ExtractMaturity(asset.Information);
            if (maturity is null) continue;

            var amount = asset.CurrentTotal;
            if (amount is null) continue;

            if (!periodMap.TryGetValue(maturity.Value, out var items))
            {
                items = [];
                periodMap[maturity.Value] = items;
            }
            items.Add(new BondScheduleItemDto(asset.Name, amount.Value));
        }

        return periodMap
            .Select(kv => new BondScheduleDto(kv.Key.Year, kv.Key.Month, kv.Value.Sum(i => i.Amount), kv.Value))
            .OrderBy(d => d.Year).ThenBy(d => d.Month)
            .ToArray();
    }

    // Extract a MM/YYYY maturity date isolated in the free-text Information field
    internal static (int Year, int Month)? ExtractMaturity(string information)
    {
        if (string.IsNullOrWhiteSpace(information)) return null;

        var match = MaturityRegex().Match(information);
        if (!match.Success) return null;

        return (int.Parse(match.Groups[2].Value), int.Parse(match.Groups[1].Value));
    }

    [GeneratedRegex(@"\b(0[1-9]|1[0-2])/(20\d{2})\b")]
    private static partial Regex MaturityRegex();
}

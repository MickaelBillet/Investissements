using System.Text.RegularExpressions;
using InvestissementsDashboard.Shared.Models;
using Microsoft.Extensions.Logging;

namespace InvestissementsDashboard.Api.Services;

internal sealed partial class BondScheduleService(
    IAssetsService assetsService,
    TimeProvider timeProvider,
    ILogger<BondScheduleService> logger) : IBondScheduleService
{
    public async Task<IReadOnlyList<BondScheduleDto>> GetScheduleAsync(CancellationToken ct = default)
    {
        var assets = await assetsService.GetAllAsync(ct);
        var now = timeProvider.GetUtcNow();
        var currentMonth = (now.Year, now.Month);

        var periodMap = new Dictionary<(int Year, int Month), List<BondScheduleItemDto>>();

        foreach (var asset in assets)
        {
            var maturity = ExtractMaturity(asset.Information);
            if (maturity is null) continue;

            var amount = asset.CurrentTotal;
            if (amount is null) continue;

            var couponMonths = GetCouponMonths(maturity.Value, currentMonth);
            var coupon = GetCoupon(asset, amount.Value);

            // The capital and the last coupon fall in the maturity month: one line carries both.
            AddItem(periodMap, maturity.Value, new BondScheduleItemDto(asset.Name, amount.Value,
                coupon is not null && couponMonths.Contains(maturity.Value) ? coupon.Value : 0m));

            if (coupon is null) continue;

            foreach (var month in couponMonths.Where(m => m != maturity.Value))
            {
                AddItem(periodMap, month, new BondScheduleItemDto(asset.Name, 0m, coupon.Value));
            }
        }

        return periodMap
            .Select(kv => new BondScheduleDto(kv.Key.Year, kv.Key.Month, kv.Value.Sum(i => i.Amount), kv.Value, kv.Value.Sum(i => i.Coupon)))
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

    // The sheet has no payment frequency: coupons are assumed annual, paid on each anniversary of the maturity
    // month. Only the coming ones count (from the current month), the past ones are already received.
    internal static IReadOnlyList<(int Year, int Month)> GetCouponMonths((int Year, int Month) maturity, (int Year, int Month) currentMonth)
    {
        var months = new List<(int Year, int Month)>();
        for (var month = maturity; month.CompareTo(currentMonth) >= 0; month = (month.Year - 1, month.Month))
        {
            months.Add(month);
        }

        months.Reverse();
        return months;
    }

    private decimal? GetCoupon(AssetDto asset, decimal amount)
    {
        if (asset.CouponRate is not { } rate || rate <= 0m || amount <= 0m) return null;

        // A rate above 100 % means the cell was typed as plain text ("8,5") instead of a percentage:
        // better no coupon than one a hundred times too large.
        if (rate > 1m)
        {
            logger.LogWarning("Coupon rate {Rate} of '{Asset}' is above 100 %: check that the cell is formatted as a percentage.", rate, asset.Name);
            return null;
        }

        return Math.Round(amount * rate, 2);
    }

    private static void AddItem(Dictionary<(int Year, int Month), List<BondScheduleItemDto>> periodMap, (int Year, int Month) period, BondScheduleItemDto item)
    {
        if (!periodMap.TryGetValue(period, out var items))
        {
            items = [];
            periodMap[period] = items;
        }
        items.Add(item);
    }

    [GeneratedRegex(@"\b(0[1-9]|1[0-2])/(20\d{2})\b")]
    private static partial Regex MaturityRegex();
}

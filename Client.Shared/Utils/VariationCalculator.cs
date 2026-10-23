using InvestissementsDashboard.Shared.Models;

namespace InvestissementsDashboard.Client.Utils;

/// <summary>
/// Percentage change between the latest point of a history and a past reference point.
/// Returns null when the history is too short, no reference exists, or the reference value is zero.
/// </summary>
public static class VariationCalculator
{
    public static decimal? SinceDaysBack(IReadOnlyList<SnapshotDto> history, int daysBack) =>
        Compute(history, s => s.NetCapital, h => RefDaysBack(h, s => s.Date, daysBack));

    public static decimal? SinceYearStart(IReadOnlyList<SnapshotDto> history) =>
        Compute(history, s => s.NetCapital, h => RefYearStart(h, s => s.Date));

    // The ROIC series is TWR-chained, so a sub-period return is ratio-invariant to the series' T0 reference date.
    public static decimal? SinceDaysBack(IReadOnlyList<PerformancePointDto> history, int daysBack) =>
        Compute(history, p => p.ROIC, h => RefDaysBack(h, p => p.Date, daysBack));

    public static decimal? SinceYearStart(IReadOnlyList<PerformancePointDto> history) =>
        Compute(history, p => p.ROIC, h => RefYearStart(h, p => p.Date));

    private static decimal? Compute<T>(
        IReadOnlyList<T> history, Func<T, decimal> value, Func<IReadOnlyList<T>, T?> referenceOf)
        where T : class
    {
        if (history.Count < 2) return null;
        var last      = value(history[^1]);
        var reference = referenceOf(history);
        if (reference is null) return null;
        var referenceValue = value(reference);
        if (referenceValue == 0) return null;
        return (last - referenceValue) / referenceValue * 100m;
    }

    // One day back means "the previous point" rather than "exactly yesterday": there is no snapshot on weekends.
    private static T? RefDaysBack<T>(IReadOnlyList<T> history, Func<T, DateOnly> date, int daysBack) where T : class =>
        daysBack == 1
            ? history[^2]
            : history.LastOrDefault(p => date(p) <= date(history[^1]).AddDays(-daysBack));

    // YTD reference: first point of the latest point's year.
    private static T? RefYearStart<T>(IReadOnlyList<T> history, Func<T, DateOnly> date) where T : class =>
        history.FirstOrDefault(p => date(p).Year == date(history[^1]).Year);
}

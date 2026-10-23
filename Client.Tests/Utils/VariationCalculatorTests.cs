using InvestissementsDashboard.Client.Tests.Helpers;
using InvestissementsDashboard.Client.Utils;
using InvestissementsDashboard.Shared.Models;

namespace InvestissementsDashboard.Client.Tests.Utils;

public class VariationCalculatorTests
{
    private static SnapshotDto Snap(int year, int month, int day, decimal netCapital) =>
        TestData.Snapshot(date: new DateOnly(year, month, day), netCapital: netCapital);

    private static PerformancePointDto Point(int year, int month, int day, decimal roic) =>
        TestData.PerformancePoint(date: new DateOnly(year, month, day), roic: roic);

    // ── SinceDaysBack (snapshots) ─────────────────────────────────────────────

    [Fact]
    public void SinceDaysBack_WhenHistoryEmpty_ReturnsNull()
    {
        Assert.Null(VariationCalculator.SinceDaysBack((IReadOnlyList<SnapshotDto>)[], 1));
    }

    [Fact]
    public void SinceDaysBack_WhenOnlyOneEntry_ReturnsNull()
    {
        Assert.Null(VariationCalculator.SinceDaysBack([Snap(2026, 5, 20, 50_000m)], 1));
    }

    [Fact]
    public void SinceDaysBack_WhenOneDay_ComparesWithPreviousEntryWhateverItsDate()
    {
        // Friday → Monday: the previous point is three days old, it is still the daily reference.
        var history = new[] { Snap(2026, 5, 15, 50_000m), Snap(2026, 5, 18, 51_000m) };

        Assert.Equal(2m, VariationCalculator.SinceDaysBack(history, 1));
    }

    [Fact]
    public void SinceDaysBack_WhenReferenceIsZero_ReturnsNull()
    {
        var history = new[] { Snap(2026, 5, 19, 0m), Snap(2026, 5, 20, 51_000m) };

        Assert.Null(VariationCalculator.SinceDaysBack(history, 1));
    }

    [Fact]
    public void SinceDaysBack_WhenEntryExactlyNDaysBack_UsesIt()
    {
        var history = new[] { Snap(2026, 5, 13, 50_000m), Snap(2026, 5, 17, 51_000m), Snap(2026, 5, 20, 52_000m) };

        Assert.Equal(4m, VariationCalculator.SinceDaysBack(history, 7));
    }

    [Fact]
    public void SinceDaysBack_WhenSeveralEntriesOldEnough_UsesTheLatestOfThem()
    {
        var history = new[] { Snap(2026, 4, 1, 40_000m), Snap(2026, 4, 20, 50_000m), Snap(2026, 5, 20, 55_000m) };

        Assert.Equal(10m, VariationCalculator.SinceDaysBack(history, 30));
    }

    [Fact]
    public void SinceDaysBack_WhenNoEntryOldEnough_ReturnsNull()
    {
        var history = new[] { Snap(2026, 5, 15, 50_000m), Snap(2026, 5, 20, 52_000m) };

        Assert.Null(VariationCalculator.SinceDaysBack(history, 7));
    }

    // ── SinceYearStart (snapshots) ────────────────────────────────────────────

    [Fact]
    public void SinceYearStart_UsesFirstEntryOfTheLatestEntryYear()
    {
        var history = new[]
        {
            Snap(2025, 12, 31, 30_000m), Snap(2026, 1, 2, 40_000m), Snap(2026, 3, 15, 45_000m), Snap(2026, 5, 20, 48_000m)
        };

        Assert.Equal(20m, VariationCalculator.SinceYearStart(history));
    }

    [Fact]
    public void SinceYearStart_WhenSingleEntryInYear_ReturnsZero()
    {
        var history = new[] { Snap(2025, 12, 31, 30_000m), Snap(2026, 5, 20, 48_000m) };

        Assert.Equal(0m, VariationCalculator.SinceYearStart(history));
    }

    // ── Performance (ROIC) overloads ──────────────────────────────────────────

    [Fact]
    public void SinceDaysBack_WhenPerformanceHistory_UsesRoic()
    {
        var history = new[] { Point(2026, 5, 19, 100.00m), Point(2026, 5, 20, 103.20m) };

        Assert.Equal(3.20m, VariationCalculator.SinceDaysBack(history, 1));
    }

    [Fact]
    public void SinceDaysBack_WhenPerformanceReferenceIsZero_ReturnsNull()
    {
        var history = new[] { Point(2026, 5, 19, 0m), Point(2026, 5, 20, 103.20m) };

        Assert.Null(VariationCalculator.SinceDaysBack(history, 1));
    }

    [Fact]
    public void SinceYearStart_WhenPerformanceHistory_UsesFirstPointOfYear()
    {
        var history = new[] { Point(2025, 12, 31, 80m), Point(2026, 1, 2, 100m), Point(2026, 5, 20, 150m) };

        Assert.Equal(50m, VariationCalculator.SinceYearStart(history));
    }
}

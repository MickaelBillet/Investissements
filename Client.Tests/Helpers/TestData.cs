using InvestissementsDashboard.Shared.Models;

namespace InvestissementsDashboard.Client.Tests.Helpers;

internal static class TestData
{
    public static AssetDto Asset(
        string  name         = "Test",
        string  assetClass   = "Stocks",
        string  supportType  = "PEA",
        string  support      = "PEA TR",
        string  assetType    = "ETF_Stocks",
        string  sector       = "",
        string  information  = "",
        string  geography    = "",
        int     risk         = 3,
        decimal currentTotal = 1000m,
        decimal? unrealizedGain = null,
        decimal? roi         = null,
        decimal? yield       = null) =>
        new(1, name, assetClass, supportType, support, assetType, sector, information, geography,
            risk, null, null, null, currentTotal, unrealizedGain, yield, roi, 0m);

    public static SnapshotDto Snapshot(
        DateOnly? date           = null,
        decimal   portfolio      = 10_000m,
        decimal?  lifeStrategy   = 100m,
        decimal?  msciWorld      = 100m,
        decimal   totalPurchases = 10_000m,
        decimal   totalReturns   = 0m) =>
        new(date ?? new DateOnly(2025, 1, 1), portfolio, lifeStrategy, msciWorld, totalPurchases, totalReturns);

    public static PerformancePointDto PerformancePoint(
        DateOnly? date          = null,
        decimal   portfolio     = 100m,
        decimal?  lifeStrategy  = 100m,
        decimal?  msciWorld     = 100m) =>
        new(date ?? new DateOnly(2025, 1, 1), portfolio, lifeStrategy, msciWorld);
}

using InvestissementsDashboard.Client.Resources;
using InvestissementsDashboard.Client.Services;
using InvestissementsDashboard.Shared.Models;
using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace InvestissementsDashboard.Client.Tests.Helpers;

internal static class TestData
{
    private static readonly System.Resources.ResourceManager _rm = new(
        "InvestissementsDashboard.Client.Resources.Translations",
        typeof(Translations).Assembly);

    public static void AddLocalizationMock(this IServiceCollection services)
    {
        var mock = new Mock<ILocalizationService>();
        mock.Setup(l => l.Translate(It.IsAny<string>()))
            .Returns<string>(key => _rm.GetString(key) ?? key);
        services.AddSingleton(mock.Object);
    }

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
        decimal   roic          = 100m,
        decimal?  lifeStrategy  = 100m,
        decimal?  msciWorld     = 100m) =>
        new(date ?? new DateOnly(2025, 1, 1), roic, lifeStrategy, msciWorld);
}

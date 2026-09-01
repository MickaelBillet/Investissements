using InvestissementsDashboard.Api.Services;
using InvestissementsDashboard.Shared.Models;
using Moq;
using Xunit;

namespace InvestissementsDashboard.Api.Tests.Services;

public class BondScheduleServiceTests
{
    private static BondScheduleService CreateService(Mock<IAssetsService> mock) => new(mock.Object);

    private static Mock<IAssetsService> MockAssets(params AssetDto[] assets)
    {
        var mock = new Mock<IAssetsService>();
        mock.Setup(s => s.GetAllAsync(It.IsAny<CancellationToken>())).ReturnsAsync(assets);
        return mock;
    }

    private static AssetDto Asset(string information, decimal? totalPurchases, string name = "Test") =>
        new(1, name, "Bonds", "CTO", "CTO TR", "MarketBonds", "", information, "", 2,
            totalPurchases, null, null, totalPurchases, null, null, null, 0m);

    // ── ExtractYear ────────────────────────────────────────────────────────────

    [Fact]
    public void ExtractYear_YearIsolatedInText_ReturnsYear()
    {
        Assert.Equal(2027, BondScheduleService.ExtractYear("Obligation Renault, échéance 2027"));
    }

    [Fact]
    public void ExtractYear_NoYearInText_ReturnsNull()
    {
        Assert.Null(BondScheduleService.ExtractYear("Obligation Renault, coupon 4%"));
    }

    [Fact]
    public void ExtractYear_EmptyString_ReturnsNull()
    {
        Assert.Null(BondScheduleService.ExtractYear(""));
    }

    // ── GetScheduleAsync ──────────────────────────────────────────────────────

    [Fact]
    public async Task GetScheduleAsync_AssetWithYear_IsGroupedUnderThatYear()
    {
        var svc = CreateService(MockAssets(Asset("échéance 2027", 1000m)));

        var result = await svc.GetScheduleAsync();

        Assert.Single(result);
        Assert.Equal(2027, result[0].Year);
        Assert.Equal(1000m, result[0].Amount);
    }

    [Fact]
    public async Task GetScheduleAsync_AssetWithoutYear_IsExcluded()
    {
        var svc = CreateService(MockAssets(Asset("Pas d'échéance", 1000m)));

        var result = await svc.GetScheduleAsync();

        Assert.Empty(result);
    }

    [Fact]
    public async Task GetScheduleAsync_TwoBondsSameYear_AmountsAreSummed()
    {
        var svc = CreateService(MockAssets(
            Asset("échéance 2027", 1000m),
            Asset("échéance 2027", 500m)));

        var result = await svc.GetScheduleAsync();

        Assert.Single(result);
        Assert.Equal(1500m, result[0].Amount);
    }

    [Fact]
    public async Task GetScheduleAsync_AssetWithYear_BondsContainsAssetNameAndAmount()
    {
        var svc = CreateService(MockAssets(Asset("échéance 2027", 1000m, "Renault 2027")));

        var result = await svc.GetScheduleAsync();

        Assert.Single(result[0].Bonds);
        Assert.Equal("Renault 2027", result[0].Bonds[0].Name);
        Assert.Equal(1000m, result[0].Bonds[0].Amount);
    }

    [Fact]
    public async Task GetScheduleAsync_TwoBondsSameYear_BondsListsBothWithIndividualAmounts()
    {
        var svc = CreateService(MockAssets(
            Asset("échéance 2027", 1000m, "Renault 2027"),
            Asset("échéance 2027", 500m, "Orange 2027")));

        var result = await svc.GetScheduleAsync();

        Assert.Equal(2, result[0].Bonds.Count);
        Assert.Contains(result[0].Bonds, b => b.Name == "Renault 2027" && b.Amount == 1000m);
        Assert.Contains(result[0].Bonds, b => b.Name == "Orange 2027" && b.Amount == 500m);
    }

    [Fact]
    public async Task GetScheduleAsync_MultipleYears_ResultIsSortedAscending()
    {
        var svc = CreateService(MockAssets(
            Asset("échéance 2030", 1000m),
            Asset("échéance 2025", 500m),
            Asset("échéance 2027", 700m)));

        var result = await svc.GetScheduleAsync();

        Assert.Equal([2025, 2027, 2030], result.Select(d => d.Year));
    }
}

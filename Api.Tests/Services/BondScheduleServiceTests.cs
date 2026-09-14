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

    // ── ExtractMaturity ───────────────────────────────────────────────────────

    [Fact]
    public void ExtractMaturity_MaturityIsolatedInText_ReturnsYearAndMonth()
    {
        Assert.Equal((2027, 5), BondScheduleService.ExtractMaturity("Obligation Renault, échéance 05/2027"));
    }

    [Fact]
    public void ExtractMaturity_YearOnlyInText_ReturnsNull()
    {
        Assert.Null(BondScheduleService.ExtractMaturity("Obligation Renault, échéance 2027"));
    }

    [Fact]
    public void ExtractMaturity_NoMaturityInText_ReturnsNull()
    {
        Assert.Null(BondScheduleService.ExtractMaturity("Obligation Renault, coupon 4%"));
    }

    [Fact]
    public void ExtractMaturity_EmptyString_ReturnsNull()
    {
        Assert.Null(BondScheduleService.ExtractMaturity(""));
    }

    // ── GetScheduleAsync ──────────────────────────────────────────────────────

    [Fact]
    public async Task GetScheduleAsync_AssetWithMaturity_IsGroupedUnderThatMonthAndYear()
    {
        var svc = CreateService(MockAssets(Asset("échéance 05/2027", 1000m)));

        var result = await svc.GetScheduleAsync();

        Assert.Single(result);
        Assert.Equal(5, result[0].Month);
        Assert.Equal(2027, result[0].Year);
        Assert.Equal(1000m, result[0].Amount);
    }

    [Fact]
    public async Task GetScheduleAsync_AssetWithoutMaturity_IsExcluded()
    {
        var svc = CreateService(MockAssets(Asset("Pas d'échéance", 1000m)));

        var result = await svc.GetScheduleAsync();

        Assert.Empty(result);
    }

    [Fact]
    public async Task GetScheduleAsync_AssetWithYearOnly_IsExcluded()
    {
        var svc = CreateService(MockAssets(Asset("échéance 2027", 1000m)));

        var result = await svc.GetScheduleAsync();

        Assert.Empty(result);
    }

    [Fact]
    public async Task GetScheduleAsync_TwoBondsSameMonthAndYear_AmountsAreSummed()
    {
        var svc = CreateService(MockAssets(
            Asset("échéance 05/2027", 1000m),
            Asset("échéance 05/2027", 500m)));

        var result = await svc.GetScheduleAsync();

        Assert.Single(result);
        Assert.Equal(1500m, result[0].Amount);
    }

    [Fact]
    public async Task GetScheduleAsync_TwoBondsSameYearDifferentMonth_AreNotMerged()
    {
        var svc = CreateService(MockAssets(
            Asset("échéance 03/2027", 1000m),
            Asset("échéance 09/2027", 500m)));

        var result = await svc.GetScheduleAsync();

        Assert.Equal(2, result.Count);
        Assert.Equal(1000m, result[0].Amount);
        Assert.Equal(500m, result[1].Amount);
    }

    [Fact]
    public async Task GetScheduleAsync_AssetWithMaturity_BondsContainsAssetNameAndAmount()
    {
        var svc = CreateService(MockAssets(Asset("échéance 05/2027", 1000m, "Renault 2027")));

        var result = await svc.GetScheduleAsync();

        Assert.Single(result[0].Bonds);
        Assert.Equal("Renault 2027", result[0].Bonds[0].Name);
        Assert.Equal(1000m, result[0].Bonds[0].Amount);
    }

    [Fact]
    public async Task GetScheduleAsync_TwoBondsSameMonthAndYear_BondsListsBothWithIndividualAmounts()
    {
        var svc = CreateService(MockAssets(
            Asset("échéance 05/2027", 1000m, "Renault 2027"),
            Asset("échéance 05/2027", 500m, "Orange 2027")));

        var result = await svc.GetScheduleAsync();

        Assert.Equal(2, result[0].Bonds.Count);
        Assert.Contains(result[0].Bonds, b => b.Name == "Renault 2027" && b.Amount == 1000m);
        Assert.Contains(result[0].Bonds, b => b.Name == "Orange 2027" && b.Amount == 500m);
    }

    [Fact]
    public async Task GetScheduleAsync_MultipleMaturities_ResultIsSortedByYearThenMonth()
    {
        var svc = CreateService(MockAssets(
            Asset("échéance 01/2030", 1000m),
            Asset("échéance 09/2025", 500m),
            Asset("échéance 03/2027", 700m),
            Asset("échéance 01/2025", 300m)));

        var result = await svc.GetScheduleAsync();

        Assert.Equal(
            [(2025, 1), (2025, 9), (2027, 3), (2030, 1)],
            result.Select(d => (d.Year, d.Month)));
    }
}

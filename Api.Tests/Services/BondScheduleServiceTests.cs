using InvestissementsDashboard.Api.Services;
using InvestissementsDashboard.Shared.Models;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace InvestissementsDashboard.Api.Tests.Services;

public class BondScheduleServiceTests
{
    // Every test runs on 2026-10-22: coupons are counted from October 2026 onwards.
    private static readonly DateTimeOffset Now = new(2026, 10, 22, 8, 0, 0, TimeSpan.Zero);

    private static BondScheduleService CreateService(Mock<IAssetsService> mock) =>
        new(mock.Object, new FixedTimeProvider(Now), NullLogger<BondScheduleService>.Instance);

    private static Mock<IAssetsService> MockAssets(params AssetDto[] assets)
    {
        var mock = new Mock<IAssetsService>();
        mock.Setup(s => s.GetAllAsync(It.IsAny<CancellationToken>())).ReturnsAsync(assets);
        return mock;
    }

    private static AssetDto Asset(string information, decimal? currentTotal, string name = "Test", decimal? couponRate = null) =>
        new(1, name, "Bonds", "CTO", "CTO TR", "MarketBonds", "", information, "", 2,
            currentTotal, null, null, currentTotal, null, null, null, 0m, CouponRate: couponRate);

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

    // ── GetCouponMonths ───────────────────────────────────────────────────────

    [Fact]
    public void GetCouponMonths_MaturityInFutureYears_ReturnsEachComingAnniversaryInOrder()
    {
        var result = BondScheduleService.GetCouponMonths((2029, 5), (2026, 10));

        Assert.Equal([(2027, 5), (2028, 5), (2029, 5)], result);
    }

    [Fact]
    public void GetCouponMonths_AnniversaryInCurrentMonth_IsIncluded()
    {
        var result = BondScheduleService.GetCouponMonths((2028, 10), (2026, 10));

        Assert.Equal([(2026, 10), (2027, 10), (2028, 10)], result);
    }

    [Fact]
    public void GetCouponMonths_MaturityLaterThisYear_ReturnsMaturityOnly()
    {
        var result = BondScheduleService.GetCouponMonths((2026, 12), (2026, 10));

        Assert.Equal([(2026, 12)], result);
    }

    [Fact]
    public void GetCouponMonths_MaturityAlreadyPast_ReturnsEmpty()
    {
        Assert.Empty(BondScheduleService.GetCouponMonths((2026, 3), (2026, 10)));
    }

    // ── GetScheduleAsync — capital ────────────────────────────────────────────

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

    // ── GetScheduleAsync — coupons ────────────────────────────────────────────

    [Fact]
    public async Task GetScheduleAsync_BondWithCouponRate_AddsACouponOnEachComingAnniversary()
    {
        var svc = CreateService(MockAssets(Asset("échéance 05/2028", 1000m, couponRate: 0.085m)));

        var result = await svc.GetScheduleAsync();

        Assert.Equal([(2027, 5), (2028, 5)], result.Select(d => (d.Year, d.Month)));
        Assert.All(result, d => Assert.Equal(85m, d.Coupons));
        Assert.Equal(0m, result[0].Amount);
        Assert.Equal(1000m, result[1].Amount);
    }

    [Fact]
    public async Task GetScheduleAsync_MaturityMonth_OneLineCarriesCapitalAndLastCoupon()
    {
        var svc = CreateService(MockAssets(Asset("échéance 05/2027", 1000m, "Renault 2027", couponRate: 0.085m)));

        var result = await svc.GetScheduleAsync();

        var bond = Assert.Single(Assert.Single(result).Bonds);
        Assert.Equal("Renault 2027", bond.Name);
        Assert.Equal(1000m, bond.Amount);
        Assert.Equal(85m, bond.Coupon);
    }

    [Fact]
    public async Task GetScheduleAsync_CouponOnlyMonth_LineHasZeroCapital()
    {
        var svc = CreateService(MockAssets(Asset("échéance 05/2028", 1000m, "Renault 2028", couponRate: 0.085m)));

        var result = await svc.GetScheduleAsync();

        var bond = Assert.Single(result[0].Bonds);
        Assert.Equal(0m, bond.Amount);
        Assert.Equal(85m, bond.Coupon);
    }

    [Fact]
    public async Task GetScheduleAsync_TwoBondsCouponsSameMonth_CouponsAreSummed()
    {
        var svc = CreateService(MockAssets(
            Asset("échéance 05/2028", 1000m, "Renault 2028", couponRate: 0.085m),
            Asset("échéance 05/2029", 2000m, "Orange 2029", couponRate: 0.05m)));

        var result = await svc.GetScheduleAsync();

        var may2027 = result.Single(d => (d.Year, d.Month) == (2027, 5));
        Assert.Equal(185m, may2027.Coupons);
        Assert.Equal(2, may2027.Bonds.Count);
    }

    [Fact]
    public async Task GetScheduleAsync_Coupon_IsRoundedToTwoDecimals()
    {
        var svc = CreateService(MockAssets(Asset("échéance 05/2027", 1234.56m, couponRate: 0.0675m)));

        var result = await svc.GetScheduleAsync();

        Assert.Equal(83.33m, result[0].Coupons);
    }

    [Theory]
    [InlineData(null)]
    [InlineData(0)]
    public async Task GetScheduleAsync_NoOrZeroCouponRate_CapitalOnly(int? ratePercent)
    {
        var svc = CreateService(MockAssets(Asset("échéance 05/2028", 1000m, couponRate: ratePercent / 100m)));

        var result = await svc.GetScheduleAsync();

        var only = Assert.Single(result);
        Assert.Equal(1000m, only.Amount);
        Assert.Equal(0m, only.Coupons);
    }

    [Fact]
    public async Task GetScheduleAsync_CouponRateAboveHundredPercent_IsIgnored()
    {
        // "8,5" typed as text instead of a percentage reaches the Api as 8.5.
        var svc = CreateService(MockAssets(Asset("échéance 05/2028", 1000m, couponRate: 8.5m)));

        var result = await svc.GetScheduleAsync();

        var only = Assert.Single(result);
        Assert.Equal(0m, only.Coupons);
    }

    [Fact]
    public async Task GetScheduleAsync_MaturityAlreadyPast_CapitalOnly()
    {
        var svc = CreateService(MockAssets(Asset("échéance 03/2026", 1000m, couponRate: 0.085m)));

        var result = await svc.GetScheduleAsync();

        var only = Assert.Single(result);
        Assert.Equal(1000m, only.Amount);
        Assert.Equal(0m, only.Coupons);
    }

    [Fact]
    public async Task GetScheduleAsync_ZeroCurrentTotal_NoCoupon()
    {
        var svc = CreateService(MockAssets(Asset("échéance 05/2028", 0m, couponRate: 0.085m)));

        var result = await svc.GetScheduleAsync();

        var only = Assert.Single(result);
        Assert.Equal(0m, only.Coupons);
    }

    private sealed class FixedTimeProvider(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;
    }
}

using InvestissementsDashboard.Shared.Models;

namespace InvestissementsDashboard.Client.Model;

// Amount is the capital repaid over the period, Coupons the coupons received over it.
public record BondSchedulePeriodDto(
    int Year,
    int? Quarter,
    decimal Amount,
    IReadOnlyList<BondScheduleItemDto> Bonds,
    decimal Coupons = 0m
)
{
    public string Label => Quarter is { } q ? $"T{q} {Year}" : Year.ToString();

    public decimal Total => Amount + Coupons;
}

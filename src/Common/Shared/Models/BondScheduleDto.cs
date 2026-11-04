using System.Text.Json.Serialization;

namespace InvestissementsDashboard.Shared.Models;

// Amount is the capital repaid in the month; Coupons the coupons paid in the same month.
public record BondScheduleDto(
    int Year,
    int Month,
    decimal Amount,
    IReadOnlyList<BondScheduleItemDto> Bonds,
    decimal Coupons = 0m
);

public record BondScheduleItemDto(
    string Name,
    decimal Amount,
    decimal Coupon = 0m
)
{
    // Derived for display only: kept out of the Api's JSON, which already carries both parts.
    [JsonIgnore]
    public decimal Total => Amount + Coupon;
}

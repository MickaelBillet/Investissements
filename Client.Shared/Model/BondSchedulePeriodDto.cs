using InvestissementsDashboard.Shared.Models;

namespace InvestissementsDashboard.Client.Model;

public record BondSchedulePeriodDto(
    int Year,
    int? Quarter,
    decimal Amount,
    IReadOnlyList<BondScheduleItemDto> Bonds
)
{
    public string Label => Quarter is { } q ? $"T{q} {Year}" : Year.ToString();
}

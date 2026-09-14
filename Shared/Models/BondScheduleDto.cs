namespace InvestissementsDashboard.Shared.Models;

public record BondScheduleDto(
    int Year,
    int Month,
    decimal Amount,
    IReadOnlyList<BondScheduleItemDto> Bonds
);

public record BondScheduleItemDto(
    string Name,
    decimal Amount
);

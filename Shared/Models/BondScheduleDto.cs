namespace InvestissementsDashboard.Shared.Models;

public record BondScheduleDto(
    int Year,
    decimal Amount,
    IReadOnlyList<BondScheduleItemDto> Bonds
);

public record BondScheduleItemDto(
    string Name,
    decimal Amount
);

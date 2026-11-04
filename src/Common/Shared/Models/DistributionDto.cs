namespace InvestissementsDashboard.Shared.Models;

public record DistributionDto(
    string Name,
    decimal CurrentTotal,
    decimal WeightInPortfolio,
    int? Id = null
);

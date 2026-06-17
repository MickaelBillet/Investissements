namespace InvestissementsDashboard.Shared.Models;

public record PortfolioMetricsDto(
    decimal? RoiOnCapitalEngaged,
    decimal? AverageRisk
);

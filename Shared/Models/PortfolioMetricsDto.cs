namespace InvestissementsDashboard.Shared.Models;

public record PortfolioMetricsDto(
    decimal? RoiOnTotalPurchases,
    decimal? RoiOnCapitalEngaged,
    decimal? AverageRisk
);

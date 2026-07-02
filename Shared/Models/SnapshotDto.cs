namespace InvestissementsDashboard.Shared.Models;

public record SnapshotDto(
    DateOnly Date,
    decimal PortfolioTotal,
    decimal? LifeStrategy,
    decimal? MsciWorld,
    decimal TotalPurchases,
    decimal TotalReturns
);

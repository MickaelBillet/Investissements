namespace InvestissementsDashboard.Shared.Models;

public record SnapshotDto(
    DateOnly Date,
    decimal PortfolioTotal,
    decimal? LifeStrategy60,
    decimal? MsciWorld,
    decimal TotalPurchases,
    decimal TotalReturns
);

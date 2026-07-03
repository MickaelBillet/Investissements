namespace InvestissementsDashboard.Shared.Models;

public record SnapshotDto(
    DateOnly Date,
    decimal  NetCapital,
    decimal? LifeStrategy,
    decimal? MsciWorld,
    decimal  TotalPurchases,
    decimal  TotalReturns,
    decimal? TotalSales
);

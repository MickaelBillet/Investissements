namespace InvestissementsDashboard.Shared.Models;

public record AggregateDto(
    string Name,
    decimal? TotalPurchases,
    decimal? TotalSales,
    decimal? Dividends,
    decimal CurrentTotal,
    bool HasIncompleteData,
    decimal? UnrealizedGain,
    decimal? Yield,
    decimal? Roi,
    decimal WeightInGroup,
    decimal WeightInPortfolio
);

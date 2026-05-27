namespace InvestissementsDashboard.Shared.Models;

public record AssetDto(
    int Id,
    string Name,
    string AssetClass,
    string SupportType,
    string Support,
    string AssetType,
    string Sector,
    string Information,
    string Geography,
    int Risk,
    decimal? TotalPurchases,
    decimal? TotalSales,
    decimal? Dividends,
    decimal? CurrentTotal,
    decimal? UnrealizedGain,
    decimal? Yield,
    decimal? Roi,
    decimal WeightInPortfolio
);

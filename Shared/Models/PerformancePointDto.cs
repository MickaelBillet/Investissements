namespace InvestissementsDashboard.Shared.Models;

public record PerformancePointDto(
    DateOnly Date,
    decimal  ROI,
    decimal  ROIC,
    decimal? LifeStrategy60,
    decimal? MsciWorld);

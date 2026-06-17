namespace InvestissementsDashboard.Shared.Models;

public record PerformancePointDto(
    DateOnly Date,
    decimal  ROIC,
    decimal? LifeStrategy60,
    decimal? MsciWorld);

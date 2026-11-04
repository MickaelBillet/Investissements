namespace InvestissementsDashboard.Shared.Models;

public record PerformancePointDto(
    DateOnly Date,
    decimal  ROIC,
    decimal? LifeStrategy,
    decimal? MsciWorld);

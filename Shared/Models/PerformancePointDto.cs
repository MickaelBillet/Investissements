namespace InvestissementsDashboard.Shared.Models;

public record PerformancePointDto(
    DateOnly Date,
    decimal  Portfolio,
    decimal? LifeStrategy60,
    decimal? MsciWorld);

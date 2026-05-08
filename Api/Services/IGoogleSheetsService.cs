namespace InvestissementsDashboard.Api.Services;

public interface IGoogleSheetsService
{
    Task<IReadOnlyList<IReadOnlyList<string>>> GetRangeAsync(string range, CancellationToken ct = default);
}

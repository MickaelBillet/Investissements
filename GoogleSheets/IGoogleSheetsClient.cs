namespace InvestissementsDashboard.GoogleSheets;

public interface IGoogleSheetsClient
{
    Task<IReadOnlyList<IReadOnlyList<object>>> GetRangeAsync(string sheetName, CancellationToken ct = default);
}

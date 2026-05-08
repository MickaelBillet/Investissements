using System.Globalization;
using InvestissementsDashboard.Shared.Models;
using Microsoft.Extensions.Logging;

namespace InvestissementsDashboard.Api.Services;

internal sealed class SnapshotService : ISnapshotService
{
    private const string Range = "Snapshot";

    private readonly IGoogleSheetsService _sheetsService;
    private readonly ILogger<SnapshotService> _logger;

    public SnapshotService(IGoogleSheetsService sheetsService, ILogger<SnapshotService> logger)
    {
        _sheetsService = sheetsService;
        _logger = logger;
    }

    public async Task<SnapshotDto?> GetLastAsync(CancellationToken ct = default)
    {
        var rows = await _sheetsService.GetRangeAsync(Range, ct);
        var dataRows = rows.Skip(1).ToList();

        if (dataRows.Count == 0)
        {
            _logger.LogWarning("Snapshot sheet has no data rows.");
            return null;
        }

        return ParseRow(dataRows[^1]);
    }

    public async Task<IReadOnlyList<SnapshotDto>> GetHistoryAsync(CancellationToken ct = default)
    {
        var rows = await _sheetsService.GetRangeAsync(Range, ct);

        return rows.Skip(1)
                   .Select(ParseRow)
                   .OrderBy(s => s.Date)
                   .ToList();
    }

    private SnapshotDto ParseRow(IReadOnlyList<string> row)
    {
        return new SnapshotDto(
            Date: DateOnly.ParseExact(row[0], "yyyy-MM-dd", CultureInfo.InvariantCulture),
            PortfolioTotal: ParseDecimal(row, 1)!.Value,
            LifeStrategy60: ParseDecimal(row, 2),
            MsciWorld: ParseDecimal(row, 3),
            TotalPurchases: ParseDecimal(row, 4),
            TotalReturns: ParseDecimal(row, 5)
        );
    }

    private static decimal? ParseDecimal(IReadOnlyList<string> row, int index)
    {
        if (index >= row.Count)
            return null;

        var value = row[index];

        if (string.IsNullOrWhiteSpace(value))
            return null;

        return decimal.Parse(value, CultureInfo.InvariantCulture);
    }
}

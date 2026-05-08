using System.Globalization;
using InvestissementsDashboard.Shared.Models;
using Microsoft.Extensions.Logging;

namespace InvestissementsDashboard.Api.Services;

internal sealed class AssetsService : IAssetsService
{
    private const string Range = "Asset";
    private const string NotDefined = "Not Defined";
    private const string Nd = "ND";

    private static readonly Dictionary<string, int> DimensionColumns = new(StringComparer.OrdinalIgnoreCase)
    {
        ["assetClass"]   = 2,
        ["supportType"]  = 3,
        ["support"]      = 4,
        ["assetType"]    = 5,
    };

    private static readonly Dictionary<string, string> DimensionReferenceTabs = new(StringComparer.OrdinalIgnoreCase)
    {
        ["assetClass"]   = "AssetClass",
        ["supportType"]  = "SupportType",
        ["support"]      = "Support",
        ["assetType"]    = "AssetType",
    };

    private readonly IGoogleSheetsService _sheetsService;
    private readonly ILogger<AssetsService> _logger;

    public AssetsService(IGoogleSheetsService sheetsService, ILogger<AssetsService> logger)
    {
        _sheetsService = sheetsService;
        _logger = logger;
    }

    public async Task<IReadOnlyList<AssetDto>> GetAllAsync(CancellationToken ct = default)
    {
        var rows = await _sheetsService.GetRangeAsync(Range, ct);
        var dataRows = rows.Skip(1).Where(r => r.Count > 1 && r[1] != NotDefined).ToList();

        if (dataRows.Count == 0)
        {
            _logger.LogWarning("Asset sheet has no valid data rows.");
            return [];
        }

        var portfolioTotal = dataRows.Sum(r => ParseDecimalRequired(r, 11));

        return dataRows.Select(r => ParseRow(r, portfolioTotal)).ToList();
    }

    public async Task<IReadOnlyList<DistributionDto>> GetDistributionByDimensionAsync(string dimension, CancellationToken ct = default)
    {
        if (!DimensionColumns.TryGetValue(dimension, out var colIndex))
        {
            _logger.LogWarning("Unknown distribution dimension: '{Dimension}'.", dimension);
            return [];
        }

        var refTab = DimensionReferenceTabs[dimension];
        var assetTask = _sheetsService.GetRangeAsync(Range, ct);
        var refTask   = _sheetsService.GetRangeAsync(refTab, ct);
        await Task.WhenAll(assetTask, refTask);
        var rows    = assetTask.Result;
        var refRows = refTask.Result;

        var idByName = refRows.Skip(1)
            .Where(r => r.Count >= 2)
            .ToDictionary(r => r[1], r => int.Parse(r[0], CultureInfo.InvariantCulture), StringComparer.OrdinalIgnoreCase);

        var dataRows = rows.Skip(1).Where(r => r.Count > colIndex && r[1] != NotDefined).ToList();

        var portfolioTotal = dataRows.Sum(r => ParseDecimalRequired(r, 11));

        if (portfolioTotal == 0)
            return [];

        return dataRows
            .GroupBy(r => r[colIndex])
            .Select(g =>
            {
                var groupTotal = g.Sum(r => ParseDecimalRequired(r, 11));
                return new DistributionDto(
                    Name: g.Key,
                    CurrentTotal: groupTotal,
                    WeightInPortfolio: Math.Round(groupTotal / portfolioTotal * 100, 2),
                    Id: idByName.TryGetValue(g.Key, out var id) ? id : null
                );
            })
            .OrderByDescending(d => d.CurrentTotal)
            .ToList();
    }

    private static AssetDto ParseRow(IReadOnlyList<string> row, decimal portfolioTotal)
    {
        var totalPurchases = ParseDecimalNullable(row, 8);
        var totalSales     = ParseDecimalNullable(row, 9);
        var dividends      = ParseDecimalNullable(row, 10);
        var currentTotal   = ParseDecimalRequired(row, 11);

        var netInvested    = totalPurchases.HasValue && totalSales.HasValue
            ? totalPurchases.Value - totalSales.Value
            : (decimal?)null;

        var unrealizedGain = netInvested.HasValue
            ? Math.Round(currentTotal - netInvested.Value, 2)
            : (decimal?)null;

        var yield = dividends.HasValue && netInvested.HasValue && netInvested.Value != 0
            ? Math.Round(dividends.Value / netInvested.Value * 100, 2)
            : (decimal?)null;

        var roi = totalPurchases.HasValue && totalSales.HasValue && dividends.HasValue && totalPurchases.Value != 0
            ? Math.Round((currentTotal + totalSales.Value + dividends.Value - totalPurchases.Value) / totalPurchases.Value * 100, 2)
            : (decimal?)null;

        return new AssetDto(
            Id: int.Parse(row[0], CultureInfo.InvariantCulture),
            Name: row[1],
            AssetClass: row[2],
            SupportType: row[3],
            Support: row[4],
            AssetType: row[5],
            Information: row.Count > 6 ? row[6] : string.Empty,
            Risk: int.Parse(row[7], CultureInfo.InvariantCulture),
            TotalPurchases: totalPurchases,
            TotalSales: totalSales,
            Dividends: dividends,
            CurrentTotal: currentTotal,
            UnrealizedGain: unrealizedGain,
            Yield: yield,
            Roi: roi,
            WeightInPortfolio: portfolioTotal > 0 ? Math.Round(currentTotal / portfolioTotal * 100, 2) : 0
        );
    }

    private static decimal ParseDecimalRequired(IReadOnlyList<string> row, int index)
        => decimal.Parse(row[index], CultureInfo.InvariantCulture);

    private static decimal? ParseDecimalNullable(IReadOnlyList<string> row, int index)
    {
        if (index >= row.Count) return null;
        var value = row[index];
        if (string.IsNullOrWhiteSpace(value) || value == Nd) return null;
        return decimal.Parse(value, CultureInfo.InvariantCulture);
    }
}

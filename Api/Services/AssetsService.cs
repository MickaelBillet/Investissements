using InvestissementsDashboard.GoogleSheets;
using InvestissementsDashboard.Shared.Models;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace InvestissementsDashboard.Api.Services;

internal sealed class AssetsService : IAssetsService
{
    private const string AssetsCacheKey = "assets:getAll";
    private static readonly TimeSpan AssetsCacheTtl = TimeSpan.FromSeconds(30);
    private static readonly SemaphoreSlim AssetsCacheLock = new(1, 1);

    private const string AssetTypeReferenceCacheKey = "assettype:reference";
    private static readonly TimeSpan AssetTypeReferenceCacheTtl = TimeSpan.FromMinutes(5);
    private static readonly SemaphoreSlim AssetTypeReferenceCacheLock = new(1, 1);

    private static readonly Dictionary<string, int> DimensionColumns = new(StringComparer.OrdinalIgnoreCase)
    {
        ["assetClass"]  = SheetMappers.AssetClassColumn,
        ["supportType"] = SheetMappers.SupportTypeColumn,
        ["support"]     = SheetMappers.SupportColumn,
        ["assetType"]   = SheetMappers.AssetTypeColumn,
    };

    private readonly IGoogleSheetsClient _sheetsClient;
    private readonly IMemoryCache _cache;
    private readonly ILogger<AssetsService> _logger;

    public AssetsService(IGoogleSheetsClient sheetsClient, IMemoryCache cache, ILogger<AssetsService> logger)
    {
        _sheetsClient = sheetsClient;
        _cache        = cache;
        _logger       = logger;
    }

    // Single-flight cache: avoids the dashboard's concurrent widgets each triggering
    // their own full-sheet read and overloading the Google Sheets API quota.
    public async Task<IReadOnlyList<AssetDto>> GetAllAsync(CancellationToken ct = default)
    {
        if (_cache.TryGetValue(AssetsCacheKey, out IReadOnlyList<AssetDto>? cached) && cached is not null)
            return cached;

        await AssetsCacheLock.WaitAsync(ct);
        try
        {
            if (_cache.TryGetValue(AssetsCacheKey, out cached) && cached is not null)
                return cached;

            var rows = SheetMappers.GetAssetRows(await _sheetsClient.GetRangeAsync("Asset", ct));

            if (rows.Count == 0)
                _logger.LogWarning("Google Sheets returned no assets.");

            var portfolioTotal = SheetMappers.GetPortfolioTotal(rows);
            IReadOnlyList<AssetDto> result = rows.Select(row => SheetMappers.BuildAssetRow(row, portfolioTotal)).ToList();

            _cache.Set(AssetsCacheKey, result, AssetsCacheTtl);
            return result;
        }
        finally
        {
            AssetsCacheLock.Release();
        }
    }

    public async Task<IReadOnlyList<DistributionDto>> GetDistributionByDimensionAsync(string dimension, CancellationToken ct = default)
    {
        if (!DimensionColumns.TryGetValue(dimension, out var column))
            throw new ArgumentException(
                $"Unknown dimension '{dimension}'. Valid values: {string.Join(", ", DimensionColumns.Keys)}.",
                nameof(dimension));

        var rows = SheetMappers.GetAssetRows(await _sheetsClient.GetRangeAsync("Asset", ct));
        var portfolioTotal = SheetMappers.GetPortfolioTotal(rows);
        return SheetMappers.GetDistributionByColumn(rows, column, portfolioTotal);
    }

    public async Task<IReadOnlyList<AggregateDto>> GetEtfStocksByInformationAsync(CancellationToken ct = default)
    {
        var rows = SheetMappers.GetAssetRows(await _sheetsClient.GetRangeAsync("Asset", ct));
        var portfolioTotal = SheetMappers.GetPortfolioTotal(rows);

        var etfRows = rows.Where(row => row.Count > SheetMappers.AssetTypeColumn
            && row[SheetMappers.AssetTypeColumn]?.ToString() == "ETF_Stocks").ToList();
        if (etfRows.Count == 0) return [];

        var groupTotal = SheetMappers.SumColumn(etfRows, SheetMappers.CurrentTotalColumn);
        return SheetMappers.GroupBy(etfRows, SheetMappers.InformationColumn)
            .Select(g => SheetMappers.AggregateGroup(g.Key, g.Value, groupTotal, portfolioTotal))
            .ToList();
    }

    public async Task<IReadOnlyList<AssetDto>> GetByAssetTypeAndInformationAsync(string assetType, string information, CancellationToken ct = default)
    {
        var rows = SheetMappers.GetAssetRows(await _sheetsClient.GetRangeAsync("Asset", ct));
        var portfolioTotal = SheetMappers.GetPortfolioTotal(rows);

        var filtered = rows.Where(row =>
            row.Count > SheetMappers.InformationColumn
            && row[SheetMappers.AssetTypeColumn]?.ToString() == assetType
            && row[SheetMappers.InformationColumn]?.ToString() == information).ToList();
        if (filtered.Count == 0) return [];

        return filtered.Select(row => SheetMappers.BuildAssetRow(row, portfolioTotal)).ToList();
    }

    // Single-flight cache: AssetType reference metadata (labelFr, geoSectorEligible) is near-static, safe to cache longer than the assets list.
    public async Task<IReadOnlyList<AssetTypeReferenceDto>> GetAssetTypeReferenceAsync(CancellationToken ct = default)
    {
        if (_cache.TryGetValue(AssetTypeReferenceCacheKey, out IReadOnlyList<AssetTypeReferenceDto>? cached) && cached is not null)
            return cached;

        await AssetTypeReferenceCacheLock.WaitAsync(ct);
        try
        {
            if (_cache.TryGetValue(AssetTypeReferenceCacheKey, out cached) && cached is not null)
                return cached;

            var rows = SheetMappers.GetAssetTypeRows(await _sheetsClient.GetRangeAsync("AssetType", ct));
            IReadOnlyList<AssetTypeReferenceDto> result = rows.Select(SheetMappers.BuildAssetTypeReference).ToList();

            _cache.Set(AssetTypeReferenceCacheKey, result, AssetTypeReferenceCacheTtl);
            return result;
        }
        finally
        {
            AssetTypeReferenceCacheLock.Release();
        }
    }
}

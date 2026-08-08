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

    private static readonly Dictionary<string, string> DimensionServices = new(StringComparer.OrdinalIgnoreCase)
    {
        ["assetClass"]  = "AssetClass",
        ["supportType"] = "SupportType",
        ["support"]     = "Support",
        ["assetType"]   = "AssetType",
    };

    private readonly IAppsScriptService _appsScript;
    private readonly IMemoryCache _cache;
    private readonly ILogger<AssetsService> _logger;

    public AssetsService(IAppsScriptService appsScript, IMemoryCache cache, ILogger<AssetsService> logger)
    {
        _appsScript = appsScript;
        _cache      = cache;
        _logger     = logger;
    }

    // Single-flight cache: avoids the dashboard's concurrent widgets each triggering
    // their own full-sheet Apps Script scan and overloading the Web App under contention.
    public async Task<IReadOnlyList<AssetDto>> GetAllAsync(CancellationToken ct = default)
    {
        if (_cache.TryGetValue(AssetsCacheKey, out IReadOnlyList<AssetDto>? cached) && cached is not null)
            return cached;

        await AssetsCacheLock.WaitAsync(ct);
        try
        {
            if (_cache.TryGetValue(AssetsCacheKey, out cached) && cached is not null)
                return cached;

            var result = await _appsScript.CallAsync<IReadOnlyList<AssetDto>>("Asset", "getAll", ct);

            if (result is null || result.Count == 0)
                _logger.LogWarning("Apps Script returned no assets.");

            result ??= [];
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
        if (!DimensionServices.TryGetValue(dimension, out var service))
            throw new ArgumentException(
                $"Unknown dimension '{dimension}'. Valid values: {string.Join(", ", DimensionServices.Keys)}.",
                nameof(dimension));

        var result = await _appsScript.CallAsync<IReadOnlyList<DistributionDto>>(service, "getDistribution", ct);
        return result ?? [];
    }

    public async Task<IReadOnlyList<AggregateDto>> GetEtfStocksByInformationAsync(CancellationToken ct = default)
    {
        var result = await _appsScript.CallAsync<IReadOnlyList<AggregateDto>>("AssetType", "getEtfStocksByInformation", null, ct);
        return result ?? [];
    }

    public async Task<IReadOnlyList<AssetDto>> GetByAssetTypeAndInformationAsync(string assetType, string information, CancellationToken ct = default)
    {
        var extra = new Dictionary<string, string> { ["assetType"] = assetType, ["information"] = information };
        var result = await _appsScript.CallAsync<IReadOnlyList<AssetDto>>("AssetType", "getByAssetTypeAndInformation", extra, ct);
        return result ?? [];
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

            var result = await _appsScript.CallAsync<IReadOnlyList<AssetTypeReferenceDto>>("AssetType", "getReference", ct);
            result ??= [];
            _cache.Set(AssetTypeReferenceCacheKey, result, AssetTypeReferenceCacheTtl);
            return result;
        }
        finally
        {
            AssetTypeReferenceCacheLock.Release();
        }
    }
}

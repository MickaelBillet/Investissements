using InvestissementsDashboard.Shared.Models;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace InvestissementsDashboard.Api.Services;

internal sealed class SnapshotService : ISnapshotService
{
    private const string LastSnapshotCacheKey = "snapshot:getLast";
    private static readonly TimeSpan LastSnapshotCacheTtl = TimeSpan.FromSeconds(30);
    private static readonly SemaphoreSlim LastSnapshotCacheLock = new(1, 1);

    private const string HistoryCacheKey = "snapshot:getHistory";
    private static readonly TimeSpan HistoryCacheTtl = TimeSpan.FromMinutes(5);
    private static readonly SemaphoreSlim HistoryCacheLock = new(1, 1);

    private readonly IAppsScriptService _appsScript;
    private readonly IMemoryCache _cache;
    private readonly ILogger<SnapshotService> _logger;

    public SnapshotService(IAppsScriptService appsScript, IMemoryCache cache, ILogger<SnapshotService> logger)
    {
        _appsScript = appsScript;
        _cache      = cache;
        _logger     = logger;
    }

    // Single-flight cache: PortfolioMetricsService and the dashboard's own snapshot
    // widget both request the last snapshot concurrently — avoid firing it twice.
    public async Task<SnapshotDto?> GetLastAsync(CancellationToken ct = default)
    {
        if (_cache.TryGetValue(LastSnapshotCacheKey, out SnapshotDto? cached))
            return cached;

        await LastSnapshotCacheLock.WaitAsync(ct);
        try
        {
            if (_cache.TryGetValue(LastSnapshotCacheKey, out cached))
                return cached;

            var result = await _appsScript.CallAsync<SnapshotDto>("Snapshot", "getLast", ct);
            _cache.Set(LastSnapshotCacheKey, result, LastSnapshotCacheTtl);
            return result;
        }
        finally
        {
            LastSnapshotCacheLock.Release();
        }
    }

    // Single-flight cache: the snapshot history only changes once a day (06:00 cron),
    // so a generous TTL avoids re-hitting Apps Script on every page load.
    public async Task<IReadOnlyList<SnapshotDto>> GetHistoryAsync(CancellationToken ct = default)
    {
        if (_cache.TryGetValue(HistoryCacheKey, out IReadOnlyList<SnapshotDto>? cached) && cached is not null)
            return cached;

        await HistoryCacheLock.WaitAsync(ct);
        try
        {
            if (_cache.TryGetValue(HistoryCacheKey, out cached) && cached is not null)
                return cached;

            var result = await _appsScript.CallAsync<IReadOnlyList<SnapshotDto>>("Snapshot", "getHistory", ct);
            result ??= [];
            _cache.Set(HistoryCacheKey, result, HistoryCacheTtl);
            return result;
        }
        finally
        {
            HistoryCacheLock.Release();
        }
    }
}

using InvestissementsDashboard.Shared.Models;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace InvestissementsDashboard.Api.Services;

internal sealed class SnapshotService : ISnapshotService
{
    private const string LastSnapshotCacheKey = "snapshot:getLast";
    private static readonly TimeSpan LastSnapshotCacheTtl = TimeSpan.FromSeconds(30);
    private static readonly SemaphoreSlim LastSnapshotCacheLock = new(1, 1);

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

    public async Task<IReadOnlyList<SnapshotDto>> GetHistoryAsync(CancellationToken ct = default)
    {
        var result = await _appsScript.CallAsync<IReadOnlyList<SnapshotDto>>("Snapshot", "getHistory", ct);
        return result ?? [];
    }
}

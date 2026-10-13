using InvestissementsDashboard.Api.Mappers;
using InvestissementsDashboard.GoogleSheets;
using InvestissementsDashboard.Shared.Models;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace InvestissementsDashboard.Api.Services;

internal sealed class WatchlistService : IWatchlistService
{
    private const string SheetName = "Watchlist";
    private const string CacheKey  = "watchlist:getAll";
    private static readonly TimeSpan CacheTtl  = TimeSpan.FromMinutes(5);
    private static readonly SemaphoreSlim CacheLock = new(1, 1);

    private readonly IGoogleSheetsClient _sheetsClient;
    private readonly IMemoryCache _cache;
    private readonly ILogger<WatchlistService> _logger;

    public WatchlistService(IGoogleSheetsClient sheetsClient, IMemoryCache cache, ILogger<WatchlistService> logger)
    {
        _sheetsClient = sheetsClient;
        _cache        = cache;
        _logger       = logger;
    }

    // The watchlist is edited by hand and rarely, so a few minutes of caching spares
    // the Sheets quota without making an edit noticeably stale for an MCP client.
    public async Task<IReadOnlyList<WatchlistItemDto>> GetAllAsync(CancellationToken ct = default)
    {
        if (_cache.TryGetValue(CacheKey, out IReadOnlyList<WatchlistItemDto>? cached) && cached is not null)
            return cached;

        await CacheLock.WaitAsync(ct);
        try
        {
            if (_cache.TryGetValue(CacheKey, out cached) && cached is not null)
                return cached;

            var rows = SheetMappers.GetWatchlistRows(await _sheetsClient.GetRangeAsync(SheetName, ct));
            if (rows.Count == 0)
                _logger.LogWarning("Google Sheets returned no watchlist entries.");

            IReadOnlyList<WatchlistItemDto> result = rows.Select(SheetMappers.BuildWatchlistItem).ToList();

            _cache.Set(CacheKey, result, CacheTtl);
            return result;
        }
        finally
        {
            CacheLock.Release();
        }
    }
}

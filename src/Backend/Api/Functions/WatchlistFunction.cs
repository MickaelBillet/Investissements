using InvestissementsDashboard.Api.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace InvestissementsDashboard.Api.Functions;

/// <summary>
/// The stocks followed but not held, read by the portfolio review of the MAUI application. Protected by the
/// dashboard password like every other route.
/// </summary>
public sealed class WatchlistFunction
{
    private readonly IWatchlistService _watchlistService;
    private readonly ILogger<WatchlistFunction> _logger;

    public WatchlistFunction(IWatchlistService watchlistService, ILogger<WatchlistFunction> logger)
    {
        _watchlistService = watchlistService;
        _logger           = logger;
    }

    [Function(nameof(GetWatchlist))]
    public async Task<IActionResult> GetWatchlist(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "watchlist")] HttpRequest req,
        CancellationToken ct)
    {
        try
        {
            return new OkObjectResult(await _watchlistService.GetAllAsync(ct));
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(ex, "Failed to read the watchlist.");
            return new StatusCodeResult(StatusCodes.Status500InternalServerError);
        }
    }
}

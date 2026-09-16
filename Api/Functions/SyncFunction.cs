using InvestissementsDashboard.Api.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace InvestissementsDashboard.Api.Functions;

public sealed class SyncFunction
{
    private readonly ISyncService _syncService;
    private readonly ILogger<SyncFunction> _logger;

    public SyncFunction(ISyncService syncService, ILogger<SyncFunction> logger)
    {
        _syncService = syncService;
        _logger      = logger;
    }

    [Function(nameof(Trigger))]
    public async Task<IActionResult> Trigger(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "sync")] HttpRequest req,
        CancellationToken ct)
    {
        try
        {
            var result = await _syncService.TriggerAsync(ct);
            return new OkObjectResult(result);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(ex, "Failed to trigger manual sync.");
            return new StatusCodeResult(StatusCodes.Status500InternalServerError);
        }
    }
}

using InvestissementsDashboard.Api.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace InvestissementsDashboard.Api.Functions;

public sealed class SnapshotFunction
{
    private readonly ISnapshotService _snapshotService;
    private readonly ILogger<SnapshotFunction> _logger;

    public SnapshotFunction(ISnapshotService snapshotService, ILogger<SnapshotFunction> logger)
    {
        _snapshotService = snapshotService;
        _logger = logger;
    }

    [Function(nameof(GetLastSnapshot))]
    public async Task<IActionResult> GetLastSnapshot(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "snapshot")] HttpRequest req,
        CancellationToken ct)
    {
        try
        {
            var snapshot = await _snapshotService.GetLastAsync(ct);
            return snapshot is null ? new NotFoundResult() : new OkObjectResult(snapshot);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve last snapshot.");
            return new StatusCodeResult(StatusCodes.Status500InternalServerError);
        }
    }

    [Function(nameof(GetSnapshotHistory))]
    public async Task<IActionResult> GetSnapshotHistory(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "snapshot/history")] HttpRequest req,
        CancellationToken ct)
    {
        try
        {
            var history = await _snapshotService.GetHistoryAsync(ct);
            return new OkObjectResult(history);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve snapshot history.");
            return new StatusCodeResult(StatusCodes.Status500InternalServerError);
        }
    }
}

using InvestissementsDashboard.Api.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace InvestissementsDashboard.Api.Functions;

public sealed class BondScheduleFunction(IBondScheduleService bondScheduleService, ILogger<BondScheduleFunction> logger)
{
    [Function(nameof(GetBondSchedule))]
    public async Task<IActionResult> GetBondSchedule(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "assets/bondschedule")] HttpRequest req,
        CancellationToken ct)
    {
        try
        {
            var schedule = await bondScheduleService.GetScheduleAsync(ct);
            return new OkObjectResult(schedule);
        }
        catch (HttpRequestException ex)
        {
            logger.LogError(ex, "Failed to call Apps Script while computing bond schedule.");
            return new StatusCodeResult(StatusCodes.Status502BadGateway);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogError(ex, "Failed to compute bond schedule.");
            return new StatusCodeResult(StatusCodes.Status500InternalServerError);
        }
    }
}

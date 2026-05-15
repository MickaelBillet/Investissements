using InvestissementsDashboard.Api.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace InvestissementsDashboard.Api.Functions;

public sealed class PortfolioMetricsFunction(IPortfolioMetricsService metricsService, ILogger<PortfolioMetricsFunction> logger)
{
    [Function(nameof(GetPortfolioMetrics))]
    public async Task<IActionResult> GetPortfolioMetrics(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "portfolio/metrics")] HttpRequest req,
        CancellationToken ct)
    {
        try
        {
            var metrics = await metricsService.GetMetricsAsync(ct);
            return new OkObjectResult(metrics);
        }
        catch (HttpRequestException ex)
        {
            logger.LogError(ex, "Failed to call Apps Script while computing portfolio metrics.");
            return new StatusCodeResult(StatusCodes.Status502BadGateway);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogError(ex, "Failed to compute portfolio metrics.");
            return new StatusCodeResult(StatusCodes.Status500InternalServerError);
        }
    }
}

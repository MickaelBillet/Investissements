using InvestissementsDashboard.Api.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace InvestissementsDashboard.Api.Functions;

public sealed class GeographyFunction(IGeographyService geographyService, ILogger<GeographyFunction> logger)
{
    private static readonly HashSet<string> ValidAssetClasses = ["Stocks", "Bonds"];

    [Function(nameof(GetGeographyDistribution))]
    public async Task<IActionResult> GetGeographyDistribution(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "portfolio/geography/{assetClass}")] HttpRequest req,
        string assetClass,
        CancellationToken ct)
    {
        if (!ValidAssetClasses.Contains(assetClass))
        {
            logger.LogWarning("Invalid assetClass parameter received.");
            return new BadRequestObjectResult("Invalid parameter.");
        }

        try
        {
            var distribution = await geographyService.GetDistributionAsync(assetClass, ct);
            return new OkObjectResult(distribution);
        }
        catch (HttpRequestException ex)
        {
            logger.LogError(ex, "Failed to call Apps Script while computing geography for '{AssetClass}'.", assetClass);
            return new StatusCodeResult(StatusCodes.Status502BadGateway);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogError(ex, "Failed to compute geography distribution for '{AssetClass}'.", assetClass);
            return new StatusCodeResult(StatusCodes.Status500InternalServerError);
        }
    }
}

using InvestissementsDashboard.Api.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace InvestissementsDashboard.Api.Functions;

public sealed class AssetsFunction
{
    private readonly IAssetsService _assetsService;
    private readonly ILogger<AssetsFunction> _logger;

    public AssetsFunction(IAssetsService assetsService, ILogger<AssetsFunction> logger)
    {
        _assetsService = assetsService;
        _logger = logger;
    }

    [Function(nameof(GetAllAssets))]
    public async Task<IActionResult> GetAllAssets(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "assets")] HttpRequest req,
        CancellationToken ct)
    {
        try
        {
            var assets = await _assetsService.GetAllAsync(ct);
            return new OkObjectResult(assets);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve assets.");
            return new StatusCodeResult(StatusCodes.Status500InternalServerError);
        }
    }

    [Function(nameof(GetAssetsDistribution))]
    public async Task<IActionResult> GetAssetsDistribution(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "assets/distribution/{dimension}")] HttpRequest req,
        string dimension,
        CancellationToken ct)
    {
        try
        {
            var distribution = await _assetsService.GetDistributionByDimensionAsync(dimension, ct);

            if (distribution.Count == 0 && !IsValidDimension(dimension))
                return new BadRequestObjectResult($"Unknown dimension '{dimension}'. Valid values: assetClass, assetType, support, supportType.");

            return new OkObjectResult(distribution);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve assets distribution for dimension '{Dimension}'.", dimension);
            return new StatusCodeResult(StatusCodes.Status500InternalServerError);
        }
    }

    private static bool IsValidDimension(string dimension) =>
        dimension is "assetClass" or "assetType" or "support" or "supportType";
}

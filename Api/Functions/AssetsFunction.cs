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
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Failed to call Apps Script.");
            return new StatusCodeResult(StatusCodes.Status502BadGateway);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
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
            return new OkObjectResult(distribution);
        }
        catch (ArgumentException ex)
        {
            return new BadRequestObjectResult(ex.Message);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Failed to call Apps Script for dimension '{Dimension}'.", dimension);
            return new StatusCodeResult(StatusCodes.Status502BadGateway);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(ex, "Failed to retrieve assets distribution for dimension '{Dimension}'.", dimension);
            return new StatusCodeResult(StatusCodes.Status500InternalServerError);
        }
    }
}

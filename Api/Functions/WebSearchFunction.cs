using System.Text.Json;
using InvestissementsDashboard.Api.Services;
using InvestissementsDashboard.Shared.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace InvestissementsDashboard.Api.Functions;

/// <summary>
/// Web search for the agents of the MAUI application. Protected by the dashboard password like every other route.
/// </summary>
public sealed class WebSearchFunction
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly IWebSearchService _webSearchService;
    private readonly ILogger<WebSearchFunction> _logger;

    public WebSearchFunction(IWebSearchService webSearchService, ILogger<WebSearchFunction> logger)
    {
        _webSearchService = webSearchService;
        _logger           = logger;
    }

    [Function(nameof(Search))]
    public async Task<IActionResult> Search(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "websearch")] HttpRequest req,
        CancellationToken ct)
    {
        WebSearchRequestDto? request;
        try
        {
            request = await JsonSerializer.DeserializeAsync<WebSearchRequestDto>(req.Body, JsonOptions, ct);
        }
        catch (JsonException ex)
        {
            _logger.LogWarning(ex, "Invalid web search request body.");
            return new BadRequestObjectResult("Invalid parameter.");
        }

        if (string.IsNullOrWhiteSpace(request?.Query))
        {
            _logger.LogWarning("Web search request without a query.");
            return new BadRequestObjectResult("Invalid parameter.");
        }

        try
        {
            var results = await _webSearchService.SearchAsync(request, ct);
            return new OkObjectResult(results);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Failed to call the web search provider.");
            return new StatusCodeResult(StatusCodes.Status502BadGateway);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(ex, "Failed to run the web search.");
            return new StatusCodeResult(StatusCodes.Status500InternalServerError);
        }
    }
}

using System.Text.Json;
using InvestissementsDashboard.Api.Mcp;
using InvestissementsDashboard.Shared.Mcp;
using Microsoft.Extensions.Logging;

namespace InvestissementsDashboard.Api.Services.Mcp;

internal sealed class McpService : IMcpService
{
    private readonly IAssetsService           _assets;
    private readonly ISnapshotService         _snapshot;
    private readonly IPortfolioMetricsService _metrics;
    private readonly IGeographyService        _geography;
    private readonly ILogger<McpService>      _logger;

    public McpService(
        IAssetsService assets,
        ISnapshotService snapshot,
        IPortfolioMetricsService metrics,
        IGeographyService geography,
        ILogger<McpService> logger)
    {
        _assets    = assets;
        _snapshot  = snapshot;
        _metrics   = metrics;
        _geography = geography;
        _logger    = logger;
    }

    public Task<JsonRpcResponse> HandleAsync(JsonRpcRequest request, CancellationToken ct)
        => request.Method switch
        {
            "initialize"         => HandleInitializeAsync(request),
            "tools/list"         => HandleToolsListAsync(request),
            "tools/call"         => HandleToolsCallAsync(request, ct),
            "notifications/initialized" => Task.FromResult(Ok(request.Id, new { })),
            _                    => Task.FromResult(MethodNotFound(request.Id))
        };

    private static Task<JsonRpcResponse> HandleInitializeAsync(JsonRpcRequest request)
    {
        var result = new McpInitializeResult(
            ProtocolVersion: "2024-11-05",
            ServerInfo: new McpServerInfo("investissements-dashboard", "1.0.0"),
            Capabilities: new McpCapabilities(Tools: new { }));
        return Task.FromResult(Ok(request.Id, result));
    }

    private static Task<JsonRpcResponse> HandleToolsListAsync(JsonRpcRequest request)
        => Task.FromResult(Ok(request.Id, new McpToolsListResult(McpToolRegistry.Tools)));

    private async Task<JsonRpcResponse> HandleToolsCallAsync(JsonRpcRequest request, CancellationToken ct)
    {
        if (request.Params is not { } paramsEl)
            return Error(request.Id, JsonRpcErrors.InvalidParams, "Missing params.");

        string? toolName;
        JsonElement? arguments;
        try
        {
            toolName  = paramsEl.GetProperty("name").GetString();
            arguments = paramsEl.TryGetProperty("arguments", out var a) ? a : null;
        }
        catch (Exception ex) when (ex is KeyNotFoundException or InvalidOperationException)
        {
            return Error(request.Id, JsonRpcErrors.InvalidParams, "Invalid params structure.");
        }

        try
        {
            var text = toolName switch
            {
                "get_assets" =>
                    Serialize(await _assets.GetAllAsync(ct)),

                "get_assets_distribution" =>
                    Serialize(await _assets.GetDistributionByDimensionAsync(
                        GetRequiredString(arguments, "dimension"), ct)),

                "get_etf_stocks" =>
                    Serialize(await _assets.GetEtfStocksByInformationAsync(ct)),

                "get_portfolio_metrics" =>
                    Serialize(await _metrics.GetMetricsAsync(ct)),

                "get_portfolio_history" =>
                    Serialize(await _metrics.GetIndexedHistoryAsync(ct)),

                "get_snapshot" =>
                    Serialize(await _snapshot.GetLastAsync(ct)),

                "get_snapshot_history" =>
                    Serialize(await _snapshot.GetHistoryAsync(ct)),

                "get_geography_distribution" =>
                    Serialize(await _geography.GetDistributionAsync(
                        GetRequiredString(arguments, "assetClass"), ct)),

                _ => null
            };

            if (text is null)
                return Error(request.Id, JsonRpcErrors.MethodNotFound, $"Unknown tool '{toolName}'.");

            return Ok(request.Id, new McpToolsCallResult([new McpContent("text", text)]));
        }
        catch (ArgumentException ex)
        {
            return Error(request.Id, JsonRpcErrors.InvalidParams, ex.Message);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Upstream failure while calling tool '{Tool}'.", toolName);
            return Error(request.Id, JsonRpcErrors.InternalError, "Upstream service unavailable.");
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(ex, "Unexpected failure while calling tool '{Tool}'.", toolName);
            return Error(request.Id, JsonRpcErrors.InternalError, "Internal error.");
        }
    }

    private static string GetRequiredString(JsonElement? args, string key)
    {
        if (args is not { } el)
            throw new ArgumentException($"Missing required argument '{key}'.");
        if (!el.TryGetProperty(key, out var prop) || prop.GetString() is not { } value)
            throw new ArgumentException($"Missing or null argument '{key}'.");
        return value;
    }

    private static string Serialize<T>(T data)
        => JsonSerializer.Serialize(data, McpJsonOptions.Default);

    private static JsonRpcResponse Ok(JsonElement? id, object result)
        => new() { Id = id, Result = result };

    private static JsonRpcResponse MethodNotFound(JsonElement? id)
        => Error(id, JsonRpcErrors.MethodNotFound, "Method not found.");

    private static JsonRpcResponse Error(JsonElement? id, int code, string message)
        => new() { Id = id, Error = new JsonRpcError(code, message) };
}

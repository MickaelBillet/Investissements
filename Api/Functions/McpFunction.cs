using System.Text.Json;
using InvestissementsDashboard.Api.Services.Mcp;
using InvestissementsDashboard.Shared.Mcp;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace InvestissementsDashboard.Api.Functions;

public sealed class McpFunction
{
    private readonly IMcpService _handler;
    private readonly ILogger<McpFunction> _logger;

    public McpFunction(IMcpService handler, ILogger<McpFunction> logger)
    {
        _handler = handler;
        _logger  = logger;
    }

    [Function(nameof(McpEndpoint))]
    public async Task<IActionResult> McpEndpoint(
        [HttpTrigger(AuthorizationLevel.Function, "post", Route = "mcp")] HttpRequest req,
        CancellationToken ct)
    {
        JsonRpcRequest? rpcRequest;
        try
        {
            rpcRequest = await JsonSerializer.DeserializeAsync<JsonRpcRequest>(
                req.Body, McpJsonOptions.Default, ct);
        }
        catch (JsonException ex)
        {
            _logger.LogWarning(ex, "Failed to parse JSON-RPC request body.");
            var parseError = new JsonRpcResponse
            {
                Error = new JsonRpcError(JsonRpcErrors.ParseError, "Parse error.")
            };
            return new ContentResult { Content = JsonSerializer.Serialize(parseError, McpJsonOptions.Default), ContentType = "application/json", StatusCode = 200 };
        }

        if (rpcRequest is null)
        {
            var invalidReq = new JsonRpcResponse
            {
                Error = new JsonRpcError(JsonRpcErrors.InvalidRequest, "Invalid request.")
            };
            return new ContentResult { Content = JsonSerializer.Serialize(invalidReq, McpJsonOptions.Default), ContentType = "application/json", StatusCode = 200 };
        }

        var response = await _handler.HandleAsync(rpcRequest, ct);
        var json = JsonSerializer.Serialize(response, McpJsonOptions.Default);
        return new ContentResult { Content = json, ContentType = "application/json", StatusCode = 200 };
    }
}

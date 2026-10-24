using System.Text.Json;
using InvestissementsDashboard.Api.Services.Mcp;
using InvestissementsDashboard.Shared.Mcp;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace InvestissementsDashboard.Api.Functions;

public sealed class McpFunction
{
    private readonly IMcpService _handler;
    private readonly IConfiguration _configuration;
    private readonly ILogger<McpFunction> _logger;

    public McpFunction(IMcpService handler, IConfiguration configuration, ILogger<McpFunction> logger)
    {
        _handler       = handler;
        _configuration = configuration;
        _logger        = logger;
    }

    [Function(nameof(McpEndpoint))]
    public async Task<IActionResult> McpEndpoint(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "mcp")] HttpRequest req,
        CancellationToken ct)
    {
        if (!IsAuthorized(req))
            return new UnauthorizedResult();

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

        // A notification must not be answered with a JSON-RPC message (it has no id to answer to): the Streamable HTTP
        // transport expects an empty 202, and the client otherwise logs "Invalid JSON-RPC message format" on every connection.
        if (rpcRequest.Method.StartsWith("notifications/", StringComparison.Ordinal))
            return new StatusCodeResult(StatusCodes.Status202Accepted);

        var response = await _handler.HandleAsync(rpcRequest, ct);
        var json = JsonSerializer.Serialize(response, McpJsonOptions.Default);
        return new ContentResult { Content = json, ContentType = "application/json", StatusCode = 200 };
    }

    /// <summary>
    /// Accepts the MCP key (external clients such as Claude Code, which cannot type a password) or the dashboard
    /// password (the MAUI agents, whose user is already signed in): both already grant read access to the same data.
    /// </summary>
    private bool IsAuthorized(HttpRequest req)
    {
        var expectedKey = _configuration["MCP_API_KEY"];
        if (string.IsNullOrEmpty(expectedKey))
            return true;

        var providedKey = req.Headers["x-mcp-api-key"].FirstOrDefault()
                       ?? req.Query["key"].FirstOrDefault();
        if (providedKey == expectedKey)
            return true;

        // An unset password must never match a missing header.
        var expectedPassword = _configuration["DASHBOARD_PASSWORD"];
        return !string.IsNullOrEmpty(expectedPassword)
            && req.Headers["x-dashboard-password"].FirstOrDefault() == expectedPassword;
    }
}

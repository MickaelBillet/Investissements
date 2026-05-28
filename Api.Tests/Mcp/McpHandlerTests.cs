using System.Text.Json;
using InvestissementsDashboard.Api.Services;
using InvestissementsDashboard.Api.Services.Mcp;
using InvestissementsDashboard.Shared.Mcp;
using InvestissementsDashboard.Shared.Models;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace InvestissementsDashboard.Api.Tests.Mcp;

public class McpHandlerTests
{
    private static McpService CreateHandler(
        Mock<IAssetsService>?           assets    = null,
        Mock<ISnapshotService>?         snapshot  = null,
        Mock<IPortfolioMetricsService>? metrics   = null,
        Mock<IGeographyService>?        geography = null)
        => new(
            (assets    ?? new Mock<IAssetsService>()).Object,
            (snapshot  ?? new Mock<ISnapshotService>()).Object,
            (metrics   ?? new Mock<IPortfolioMetricsService>()).Object,
            (geography ?? new Mock<IGeographyService>()).Object,
            NullLogger<McpService>.Instance);

    private static JsonRpcRequest Request(string method, object? @params = null) => new()
    {
        Method = method,
        Id     = JsonSerializer.SerializeToElement(1),
        Params = @params is null ? null : JsonSerializer.SerializeToElement(@params)
    };

    private static T AssertSuccessResult<T>(JsonRpcResponse response)
    {
        Assert.Null(response.Error);
        Assert.NotNull(response.Result);
        var json = JsonSerializer.Serialize(response.Result);
        return JsonSerializer.Deserialize<T>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true })!;
    }

    // --- initialize ---

    [Fact]
    public async Task HandleAsync_Initialize_ReturnsProtocolVersionAndServerInfo()
    {
        var handler  = CreateHandler();
        var response = await handler.HandleAsync(Request("initialize"), default);

        var result = AssertSuccessResult<McpInitializeResult>(response);
        Assert.Equal("2024-11-05", result.ProtocolVersion);
        Assert.Equal("investissements-dashboard", result.ServerInfo.Name);
    }

    // --- tools/list ---

    [Fact]
    public async Task HandleAsync_ToolsList_ReturnsEightTools()
    {
        var handler  = CreateHandler();
        var response = await handler.HandleAsync(Request("tools/list"), default);

        var result = AssertSuccessResult<McpToolsListResult>(response);
        Assert.Equal(8, result.Tools.Count);
    }

    [Fact]
    public async Task HandleAsync_ToolsList_EachToolHasNameAndDescription()
    {
        var handler  = CreateHandler();
        var response = await handler.HandleAsync(Request("tools/list"), default);

        var result = AssertSuccessResult<McpToolsListResult>(response);
        foreach (var tool in result.Tools)
        {
            Assert.False(string.IsNullOrEmpty(tool.Name));
            Assert.False(string.IsNullOrEmpty(tool.Description));
        }
    }

    // --- tools/call happy paths ---

    [Fact]
    public async Task HandleAsync_ToolsCall_GetAssets_ReturnsSerializedContent()
    {
        var assets = new Mock<IAssetsService>();
        assets.Setup(s => s.GetAllAsync(It.IsAny<CancellationToken>()))
              .ReturnsAsync([new AssetDto(1, "MSCI World", "Stocks", "PEA", "PEA TR", "ETF_Stocks", "", "", "", 4, 5000m, 0m, 0m, 6000m, 1000m, 0m, 20m, 60m)]);

        var handler  = CreateHandler(assets: assets);
        var response = await handler.HandleAsync(Request("tools/call", new { name = "get_assets", arguments = new { } }), default);

        var result = AssertSuccessResult<McpToolsCallResult>(response);
        Assert.Single(result.Content);
        Assert.Equal("text", result.Content[0].Type);
        Assert.Contains("MSCI World", result.Content[0].Text);
    }

    [Fact]
    public async Task HandleAsync_ToolsCall_GetAssetsDistribution_PassesDimensionToService()
    {
        var assets = new Mock<IAssetsService>();
        assets.Setup(s => s.GetDistributionByDimensionAsync("assetClass", It.IsAny<CancellationToken>()))
              .ReturnsAsync([new DistributionDto("Stocks", 10000m, 80m, 0)]);

        var handler  = CreateHandler(assets: assets);
        var response = await handler.HandleAsync(
            Request("tools/call", new { name = "get_assets_distribution", arguments = new { dimension = "assetClass" } }), default);

        var result = AssertSuccessResult<McpToolsCallResult>(response);
        Assert.Single(result.Content);
        assets.Verify(s => s.GetDistributionByDimensionAsync("assetClass", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task HandleAsync_ToolsCall_GetEtfStocks_ReturnsTextContent()
    {
        var assets = new Mock<IAssetsService>();
        assets.Setup(s => s.GetEtfStocksByInformationAsync(It.IsAny<CancellationToken>()))
              .ReturnsAsync([]);

        var handler  = CreateHandler(assets: assets);
        var response = await handler.HandleAsync(Request("tools/call", new { name = "get_etf_stocks", arguments = new { } }), default);

        var result = AssertSuccessResult<McpToolsCallResult>(response);
        Assert.Equal("text", result.Content[0].Type);
    }

    [Fact]
    public async Task HandleAsync_ToolsCall_GetPortfolioMetrics_ReturnsTextContent()
    {
        var metrics = new Mock<IPortfolioMetricsService>();
        metrics.Setup(s => s.GetMetricsAsync(It.IsAny<CancellationToken>()))
               .ReturnsAsync(new PortfolioMetricsDto(12.5m, 8.3m, 3.2m));

        var handler  = CreateHandler(metrics: metrics);
        var response = await handler.HandleAsync(Request("tools/call", new { name = "get_portfolio_metrics", arguments = new { } }), default);

        var result = AssertSuccessResult<McpToolsCallResult>(response);
        Assert.Contains("12.5", result.Content[0].Text);
    }

    [Fact]
    public async Task HandleAsync_ToolsCall_GetPortfolioHistory_ReturnsTextContent()
    {
        var metrics = new Mock<IPortfolioMetricsService>();
        metrics.Setup(s => s.GetIndexedHistoryAsync(It.IsAny<CancellationToken>()))
               .ReturnsAsync([]);

        var handler  = CreateHandler(metrics: metrics);
        var response = await handler.HandleAsync(Request("tools/call", new { name = "get_portfolio_history", arguments = new { } }), default);

        var result = AssertSuccessResult<McpToolsCallResult>(response);
        Assert.Equal("text", result.Content[0].Type);
    }

    [Fact]
    public async Task HandleAsync_ToolsCall_GetSnapshot_ReturnsTextContent()
    {
        var snapshot = new Mock<ISnapshotService>();
        snapshot.Setup(s => s.GetLastAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(new SnapshotDto(new DateOnly(2025, 5, 1), 50000m, null, null, 40000m, 10000m));

        var handler  = CreateHandler(snapshot: snapshot);
        var response = await handler.HandleAsync(Request("tools/call", new { name = "get_snapshot", arguments = new { } }), default);

        var result = AssertSuccessResult<McpToolsCallResult>(response);
        Assert.Contains("50000", result.Content[0].Text);
    }

    [Fact]
    public async Task HandleAsync_ToolsCall_GetSnapshotHistory_ReturnsTextContent()
    {
        var snapshot = new Mock<ISnapshotService>();
        snapshot.Setup(s => s.GetHistoryAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync([]);

        var handler  = CreateHandler(snapshot: snapshot);
        var response = await handler.HandleAsync(Request("tools/call", new { name = "get_snapshot_history", arguments = new { } }), default);

        var result = AssertSuccessResult<McpToolsCallResult>(response);
        Assert.Equal("text", result.Content[0].Type);
    }

    [Fact]
    public async Task HandleAsync_ToolsCall_GetGeographyDistribution_PassesAssetClassToService()
    {
        var geography = new Mock<IGeographyService>();
        geography.Setup(s => s.GetDistributionAsync("Stocks", It.IsAny<CancellationToken>()))
                 .ReturnsAsync([new DistributionDto("Europe", 5000m, 50m, 0)]);

        var handler  = CreateHandler(geography: geography);
        var response = await handler.HandleAsync(
            Request("tools/call", new { name = "get_geography_distribution", arguments = new { assetClass = "Stocks" } }), default);

        var result = AssertSuccessResult<McpToolsCallResult>(response);
        Assert.Single(result.Content);
        geography.Verify(s => s.GetDistributionAsync("Stocks", It.IsAny<CancellationToken>()), Times.Once);
    }

    // --- tools/call error paths ---

    [Fact]
    public async Task HandleAsync_ToolsCall_UnknownTool_ReturnsMethodNotFoundError()
    {
        var handler  = CreateHandler();
        var response = await handler.HandleAsync(
            Request("tools/call", new { name = "unknown_tool", arguments = new { } }), default);

        Assert.NotNull(response.Error);
        Assert.Equal(JsonRpcErrors.MethodNotFound, response.Error!.Code);
    }

    [Fact]
    public async Task HandleAsync_ToolsCall_MissingParams_ReturnsInvalidParamsError()
    {
        var handler  = CreateHandler();
        var response = await handler.HandleAsync(Request("tools/call"), default);

        Assert.NotNull(response.Error);
        Assert.Equal(JsonRpcErrors.InvalidParams, response.Error!.Code);
    }

    [Fact]
    public async Task HandleAsync_ToolsCall_MissingRequiredArgument_ReturnsInvalidParamsError()
    {
        var handler  = CreateHandler();
        var response = await handler.HandleAsync(
            Request("tools/call", new { name = "get_assets_distribution", arguments = new { } }), default);

        Assert.NotNull(response.Error);
        Assert.Equal(JsonRpcErrors.InvalidParams, response.Error!.Code);
    }

    [Fact]
    public async Task HandleAsync_ToolsCall_ServiceThrowsHttpRequestException_ReturnsInternalError()
    {
        var assets = new Mock<IAssetsService>();
        assets.Setup(s => s.GetAllAsync(It.IsAny<CancellationToken>()))
              .ThrowsAsync(new HttpRequestException("Apps Script unreachable"));

        var handler  = CreateHandler(assets: assets);
        var response = await handler.HandleAsync(
            Request("tools/call", new { name = "get_assets", arguments = new { } }), default);

        Assert.NotNull(response.Error);
        Assert.Equal(JsonRpcErrors.InternalError, response.Error!.Code);
    }

    // --- unknown method ---

    [Fact]
    public async Task HandleAsync_UnknownMethod_ReturnsMethodNotFoundError()
    {
        var handler  = CreateHandler();
        var response = await handler.HandleAsync(Request("does/not/exist"), default);

        Assert.NotNull(response.Error);
        Assert.Equal(JsonRpcErrors.MethodNotFound, response.Error!.Code);
        Assert.Equal(JsonSerializer.SerializeToElement(1).GetInt32(), response.Id!.Value.GetInt32());
    }
}

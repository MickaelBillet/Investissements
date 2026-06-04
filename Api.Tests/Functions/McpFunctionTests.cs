using System.Text;
using InvestissementsDashboard.Api.Functions;
using InvestissementsDashboard.Api.Services.Mcp;
using InvestissementsDashboard.Shared.Mcp;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace InvestissementsDashboard.Api.Tests.Functions;

public class McpFunctionTests
{
    private static McpFunction CreateFunction(Mock<IMcpService> mockService, string? configuredKey = null)
    {
        var mockConfig = new Mock<IConfiguration>();
        mockConfig.Setup(c => c["MCP_API_KEY"]).Returns(configuredKey);
        return new(mockService.Object, mockConfig.Object, NullLogger<McpFunction>.Instance);
    }

    private static HttpRequest MockRequest(string? apiKey = null, string body = "{}", string? queryKey = null)
    {
        var mockRequest = new Mock<HttpRequest>();
        var headers = new HeaderDictionary();
        if (apiKey is not null)
            headers["x-mcp-api-key"] = apiKey;
        mockRequest.Setup(r => r.Headers).Returns(headers);
        mockRequest.Setup(r => r.Body).Returns(new MemoryStream(Encoding.UTF8.GetBytes(body)));
        var query = new QueryCollection(
            queryKey is not null
                ? new Dictionary<string, Microsoft.Extensions.Primitives.StringValues> { ["key"] = queryKey }
                : new Dictionary<string, Microsoft.Extensions.Primitives.StringValues>());
        mockRequest.Setup(r => r.Query).Returns(query);
        return mockRequest.Object;
    }

    [Fact]
    public async Task McpEndpoint_MissingApiKey_WhenConfigured_ReturnsUnauthorized()
    {
        var mock = new Mock<IMcpService>();

        var result = await CreateFunction(mock, "secret").McpEndpoint(MockRequest(apiKey: null), CancellationToken.None);

        Assert.IsType<UnauthorizedResult>(result);
        mock.Verify(s => s.HandleAsync(It.IsAny<JsonRpcRequest>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task McpEndpoint_WrongApiKey_WhenConfigured_ReturnsUnauthorized()
    {
        var mock = new Mock<IMcpService>();

        var result = await CreateFunction(mock, "secret").McpEndpoint(MockRequest(apiKey: "wrong-key"), CancellationToken.None);

        Assert.IsType<UnauthorizedResult>(result);
        mock.Verify(s => s.HandleAsync(It.IsAny<JsonRpcRequest>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task McpEndpoint_CorrectApiKey_WhenConfigured_DelegatesToService()
    {
        var mock = new Mock<IMcpService>();
        mock.Setup(s => s.HandleAsync(It.IsAny<JsonRpcRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new JsonRpcResponse());

        var body = """{"jsonrpc":"2.0","method":"tools/list"}""";
        var result = await CreateFunction(mock, "secret").McpEndpoint(MockRequest("secret", body), CancellationToken.None);

        Assert.IsType<ContentResult>(result);
        mock.Verify(s => s.HandleAsync(It.IsAny<JsonRpcRequest>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task McpEndpoint_CorrectQueryKey_WhenConfigured_DelegatesToService()
    {
        var mock = new Mock<IMcpService>();
        mock.Setup(s => s.HandleAsync(It.IsAny<JsonRpcRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new JsonRpcResponse());

        var body = """{"jsonrpc":"2.0","method":"tools/list"}""";
        var result = await CreateFunction(mock, "secret").McpEndpoint(MockRequest(body: body, queryKey: "secret"), CancellationToken.None);

        Assert.IsType<ContentResult>(result);
        mock.Verify(s => s.HandleAsync(It.IsAny<JsonRpcRequest>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task McpEndpoint_WrongQueryKey_WhenConfigured_ReturnsUnauthorized()
    {
        var mock = new Mock<IMcpService>();

        var result = await CreateFunction(mock, "secret").McpEndpoint(MockRequest(queryKey: "wrong-key"), CancellationToken.None);

        Assert.IsType<UnauthorizedResult>(result);
        mock.Verify(s => s.HandleAsync(It.IsAny<JsonRpcRequest>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task McpEndpoint_NoConfiguredKey_AllowsRequest()
    {
        var mock = new Mock<IMcpService>();
        mock.Setup(s => s.HandleAsync(It.IsAny<JsonRpcRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new JsonRpcResponse());

        var body = """{"jsonrpc":"2.0","method":"tools/list"}""";
        var result = await CreateFunction(mock, configuredKey: null).McpEndpoint(MockRequest(apiKey: null, body), CancellationToken.None);

        Assert.IsType<ContentResult>(result);
        mock.Verify(s => s.HandleAsync(It.IsAny<JsonRpcRequest>(), It.IsAny<CancellationToken>()), Times.Once);
    }
}

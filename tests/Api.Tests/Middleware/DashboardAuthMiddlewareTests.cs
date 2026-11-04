using InvestissementsDashboard.Api.Functions;
using InvestissementsDashboard.Api.Middleware;
using Microsoft.AspNetCore.Http;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Xunit;

namespace InvestissementsDashboard.Api.Tests.Middleware;

public class DashboardAuthMiddlewareTests
{
    private const string Password = "dashboard-pwd";

    private readonly DefaultHttpContext _httpContext = new();
    private bool _nextCalled;

    private async Task InvokeAsync(string functionName, string? configuredPassword, string? providedPassword)
    {
        if (providedPassword is not null)
        {
            _httpContext.Request.Headers["x-dashboard-password"] = providedPassword;
        }

        var configuration = new Mock<IConfiguration>();
        configuration.Setup(c => c["DASHBOARD_PASSWORD"]).Returns(configuredPassword);
        var services = new ServiceCollection().AddSingleton(configuration.Object).BuildServiceProvider();

        var definition = new Mock<FunctionDefinition>();
        definition.Setup(d => d.Name).Returns(functionName);

        var context = new Mock<FunctionContext>();
        context.Setup(c => c.FunctionDefinition).Returns(definition.Object);
        context.Setup(c => c.InstanceServices).Returns(services);
        // The key under which the ASP.NET Core integration exposes the request to GetHttpContext().
        context.Setup(c => c.Items).Returns(new Dictionary<object, object> { ["HttpRequestContext"] = _httpContext });

        await new DashboardAuthMiddleware().Invoke(context.Object, _ =>
        {
            _nextCalled = true;
            return Task.CompletedTask;
        });
    }

    [Fact]
    public async Task Invoke_VerifyWithWrongPassword_ReturnsUnauthorized()
    {
        await InvokeAsync(nameof(AuthFunction.Verify), Password, "wrong");

        Assert.Equal(StatusCodes.Status401Unauthorized, _httpContext.Response.StatusCode);
        Assert.False(_nextCalled);
    }

    [Fact]
    public async Task Invoke_VerifyWithoutPassword_ReturnsUnauthorized()
    {
        await InvokeAsync(nameof(AuthFunction.Verify), Password, null);

        Assert.Equal(StatusCodes.Status401Unauthorized, _httpContext.Response.StatusCode);
        Assert.False(_nextCalled);
    }

    [Fact]
    public async Task Invoke_VerifyWithCorrectPassword_CallsFunction()
    {
        await InvokeAsync(nameof(AuthFunction.Verify), Password, Password);

        Assert.True(_nextCalled);
    }

    [Theory]
    [InlineData(null, null)]
    [InlineData(null, "anything")]
    [InlineData("", "")]
    public async Task Invoke_PasswordNotConfigured_ReturnsUnauthorized(string? configured, string? provided)
    {
        await InvokeAsync(nameof(AuthFunction.Verify), configured, provided);

        Assert.Equal(StatusCodes.Status401Unauthorized, _httpContext.Response.StatusCode);
        Assert.False(_nextCalled);
    }

    [Fact]
    public async Task Invoke_McpEndpointWithoutPassword_LeavesTheCheckToTheFunction()
    {
        await InvokeAsync(nameof(McpFunction.McpEndpoint), Password, null);

        Assert.True(_nextCalled);
    }
}

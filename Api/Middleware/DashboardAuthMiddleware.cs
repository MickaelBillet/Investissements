using InvestissementsDashboard.Api.Functions;
using Microsoft.AspNetCore.Http;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Middleware;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace InvestissementsDashboard.Api.Middleware;

public sealed class DashboardAuthMiddleware : IFunctionsWorkerMiddleware
{
    private const string PasswordHeader = "x-dashboard-password";

    private static readonly string[] ExemptFunctions =
    [
        nameof(McpFunction.McpEndpoint),
        nameof(AuthFunction.Verify)
    ];

    public async Task Invoke(FunctionContext context, FunctionExecutionDelegate next)
    {
        if (ExemptFunctions.Contains(context.FunctionDefinition.Name))
        {
            await next(context);
            return;
        }

        var httpContext = context.GetHttpContext();
        if (httpContext is not null)
        {
            var configuration = context.InstanceServices.GetRequiredService<IConfiguration>();
            var expectedPassword = configuration["DASHBOARD_PASSWORD"];
            var providedPassword = httpContext.Request.Headers[PasswordHeader].FirstOrDefault();

            if (string.IsNullOrEmpty(expectedPassword) || providedPassword != expectedPassword)
            {
                httpContext.Response.StatusCode = StatusCodes.Status401Unauthorized;
                return;
            }
        }

        await next(context);
    }
}

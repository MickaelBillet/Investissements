using InvestissementsDashboard.Shared.Mcp;

namespace InvestissementsDashboard.Api.Services.Mcp;

public interface IMcpService
{
    Task<JsonRpcResponse> HandleAsync(JsonRpcRequest request, CancellationToken ct);
}

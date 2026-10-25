using InvestissementsDashboard.Api.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace InvestissementsDashboard.Api.Functions;

/// <summary>
/// Configuration of the MAUI application's agents. Protected by the dashboard password like every other route.
/// </summary>
public sealed class AgentConfigFunction
{
    private readonly IAgentConfigService _agentConfigService;
    private readonly ILogger<AgentConfigFunction> _logger;

    public AgentConfigFunction(IAgentConfigService agentConfigService, ILogger<AgentConfigFunction> logger)
    {
        _agentConfigService = agentConfigService;
        _logger             = logger;
    }

    [Function(nameof(GetAgentConfig))]
    public IActionResult GetAgentConfig(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "agents/config")] HttpRequest req)
    {
        try
        {
            return new OkObjectResult(_agentConfigService.Get());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to read the agents configuration.");
            return new StatusCodeResult(StatusCodes.Status500InternalServerError);
        }
    }
}

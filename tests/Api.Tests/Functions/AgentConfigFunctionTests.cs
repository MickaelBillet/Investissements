using InvestissementsDashboard.Api.Functions;
using InvestissementsDashboard.Api.Services;
using InvestissementsDashboard.Shared.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace InvestissementsDashboard.Api.Tests.Functions;

public class AgentConfigFunctionTests
{
    private readonly Mock<IAgentConfigService> _service = new();

    private IActionResult Invoke() =>
        new AgentConfigFunction(_service.Object, NullLogger<AgentConfigFunction>.Instance).GetAgentConfig(new Mock<HttpRequest>().Object);

    [Fact]
    public void GetAgentConfig_Configured_ReturnsOkWithConfig()
    {
        var config = new AgentConfigDto("https://foundry.test/", "gpt-5-mini");
        _service.Setup(s => s.Get()).Returns(config);

        var ok = Assert.IsType<OkObjectResult>(Invoke());

        Assert.Same(config, ok.Value);
    }

    [Fact]
    public void GetAgentConfig_WhenServiceThrows_ReturnsInternalServerError()
    {
        _service.Setup(s => s.Get()).Throws(new InvalidOperationException("boom"));

        var status = Assert.IsType<StatusCodeResult>(Invoke());

        Assert.Equal(StatusCodes.Status500InternalServerError, status.StatusCode);
    }
}

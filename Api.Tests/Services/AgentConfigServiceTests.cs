using InvestissementsDashboard.Api.Services;
using InvestissementsDashboard.Shared.Models;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace InvestissementsDashboard.Api.Tests.Services;

public class AgentConfigServiceTests
{
    private static AgentConfigService CreateService(string? endpoint, string? model) =>
        new(new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["AGENT_FOUNDRY_ENDPOINT"] = endpoint,
                ["AGENT_FOUNDRY_MODEL"] = model,
            })
            .Build());

    [Fact]
    public void Get_Configured_ReturnsTrimmedValues()
    {
        var config = CreateService(" https://foundry.test/api/projects/p ", "gpt-5-mini").Get();

        Assert.Equal(new AgentConfigDto("https://foundry.test/api/projects/p", "gpt-5-mini"), config);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Get_NotConfigured_ReturnsNulls(string? value)
    {
        var config = CreateService(value, value).Get();

        Assert.Null(config.FoundryEndpoint);
        Assert.Null(config.Model);
    }
}

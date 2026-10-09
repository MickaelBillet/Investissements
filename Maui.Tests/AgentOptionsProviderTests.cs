using AgentAI;
using InvestissementsDashboard.Client.Services;
using InvestissementsDashboard.Maui.Services;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Xunit;

namespace InvestissementsDashboard.Maui.Tests;

public class AgentOptionsProviderTests
{
    private static readonly Uri ApiBase = new("https://invest.test/");
    private readonly Mock<IAgentSettings> _settings = new();

    private AgentOptionsProvider Create() => new(_settings.Object, ApiBase, @"C:\history");

    [Fact]
    public void Create_WithoutEndpoint_Throws()
    {
        var ex = Assert.Throws<InvalidOperationException>(() => Create().Create());

        Assert.Contains("settings page", ex.Message);
    }

    [Fact]
    public void Create_WithEndpointOnly_UsesDefaultModel()
    {
        _settings.SetupGet(s => s.FoundryEndpoint).Returns("https://foundry.test/");

        var options = Create().Create();

        Assert.Equal("https://foundry.test/", options.FoundryProjectEndpoint);
        Assert.Equal(AgentAIOptions.DefaultModel, options.Model);
    }

    [Fact]
    public void Create_WithModel_UsesIt()
    {
        _settings.SetupGet(s => s.FoundryEndpoint).Returns("https://foundry.test/");
        _settings.SetupGet(s => s.Model).Returns("gpt-x");

        Assert.Equal("gpt-x", Create().Create().Model);
    }

    [Fact]
    public void Create_DerivesMcpUrlFromApiBaseAndPassesHistoryDirectory()
    {
        _settings.SetupGet(s => s.FoundryEndpoint).Returns("https://foundry.test/");

        var options = Create().Create();

        Assert.Equal("https://invest.test/api/mcp", options.InvestZaptoMcpUrl);
        Assert.Equal(@"C:\history", options.HistoryDirectory);
    }

    [Fact]
    public void Create_WithStoredBaseUrl_DerivesMcpUrlFromIt()
    {
        _settings.SetupGet(s => s.FoundryEndpoint).Returns("https://foundry.test/");
        _settings.SetupGet(s => s.InvestZaptoBaseUrl).Returns("https://stored.test/");

        Assert.Equal("https://stored.test/api/mcp", Create().Create().InvestZaptoMcpUrl);
    }

    [Fact]
    public void Create_WithMalformedStoredBaseUrl_FallsBackToApiBase()
    {
        _settings.SetupGet(s => s.FoundryEndpoint).Returns("https://foundry.test/");
        _settings.SetupGet(s => s.InvestZaptoBaseUrl).Returns("not a url");

        Assert.Equal("https://invest.test/api/mcp", Create().Create().InvestZaptoMcpUrl);
    }

    [Fact]
    public void AddAgentAI_WithOptionsFactory_DoesNotInvokeItAtRegistration()
    {
        var invoked = false;
        var services = new ServiceCollection();

        services.AddAgentAI(_ =>
        {
            invoked = true;
            return new AgentAIOptions { FoundryProjectEndpoint = "https://foundry.test/", HistoryDirectory = "h" };
        });

        Assert.False(invoked);
    }

    [Fact]
    public void AddAgentAI_WithOptionsFactory_ResolvesOptionsFromIt()
    {
        var services = new ServiceCollection();
        services.AddAgentAI(_ => new AgentAIOptions { FoundryProjectEndpoint = "https://foundry.test/", HistoryDirectory = "h" });
        using var provider = services.BuildServiceProvider();

        Assert.Equal("https://foundry.test/", provider.GetRequiredService<AgentAIOptions>().FoundryProjectEndpoint);
    }
}

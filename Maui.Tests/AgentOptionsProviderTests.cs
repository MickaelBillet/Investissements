using System.Net;
using AgentAI;
using InvestissementsDashboard.Maui.Services;
using InvestissementsDashboard.Shared.Models;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Xunit;

namespace InvestissementsDashboard.Maui.Tests;

public class AgentOptionsProviderTests
{
    private static readonly Uri ApiBase = new("https://invest.test/");
    private readonly Mock<IAgentConfigSource> _source = new();

    private AgentOptionsProvider Create() => new(_source.Object, ApiBase, @"C:\history");

    private void SetupConfig(string? endpoint, string? model = null) =>
        _source.Setup(s => s.GetAsync(It.IsAny<CancellationToken>())).ReturnsAsync(new AgentConfigDto(endpoint, model));

    [Theory]
    [InlineData(null)]
    [InlineData("  ")]
    public async Task CreateAsync_WithoutEndpoint_ThrowsNamingTheAppSetting(string? endpoint)
    {
        SetupConfig(endpoint);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => Create().CreateAsync(default));

        Assert.Contains("AGENT_FOUNDRY_ENDPOINT", ex.Message);
    }

    [Fact]
    public async Task CreateAsync_WithEndpointOnly_UsesDefaultModel()
    {
        SetupConfig("https://foundry.test/");

        var options = await Create().CreateAsync(default);

        Assert.Equal("https://foundry.test/", options.FoundryProjectEndpoint);
        Assert.Equal(AgentAIOptions.DefaultModel, options.Model);
    }

    [Fact]
    public async Task CreateAsync_WithModel_UsesIt()
    {
        SetupConfig("https://foundry.test/", "gpt-x");

        Assert.Equal("gpt-x", (await Create().CreateAsync(default)).Model);
    }

    [Fact]
    public async Task CreateAsync_Always_DerivesMcpUrlFromApiBaseAndPassesHistoryDirectory()
    {
        SetupConfig("https://foundry.test/");

        var options = await Create().CreateAsync(default);

        Assert.Equal("https://invest.test/api/mcp", options.InvestZaptoMcpUrl);
        Assert.Equal(@"C:\history", options.HistoryDirectory);
    }

    [Fact]
    public async Task CreateAsync_ApiRefuses_PropagatesHttpRequestException()
    {
        _source.Setup(s => s.GetAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new HttpRequestException("401", null, HttpStatusCode.Unauthorized));

        var ex = await Assert.ThrowsAsync<HttpRequestException>(() => Create().CreateAsync(default));

        Assert.Equal(HttpStatusCode.Unauthorized, ex.StatusCode);
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

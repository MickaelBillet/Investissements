using AgentAI;
using Azure.AI.Projects;
using Azure.Identity;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace InvestissementsDashboard.Maui.Tests;

public class AgentFactoryTests
{
    // No request reaches Azure: the Foundry client only authenticates when an agent runs.
    private const string FoundryEndpoint = "https://foundry.test/api/projects/test";

    private static AgentAIOptions Options(string? mcpUrl = null) => new()
    {
        FoundryProjectEndpoint = FoundryEndpoint,
        InvestZaptoMcpUrl = mcpUrl,
        HistoryDirectory = Path.Combine(Path.GetTempPath(), "agentai-tests", Guid.NewGuid().ToString("N")),
    };

    private static AgentFactoryBuilder CreateBuilder() =>
        new(
            new InvestZaptoMcpClient(new HttpClient(), NullLoggerFactory.Instance),
            new Mock<INewsService>().Object,
            new Mock<IWebSearchService>().Object,
            new Mock<IWeatherService>().Object,
            TimeProvider.System,
            NullLoggerFactory.Instance);

    private static AgentFactory CreateFactory(params IAgentDefinitionBuilder[] builders)
    {
        var options = Options();
        var mcpTools = new InvestZaptoMcpToolSource(new InvestZaptoMcpClient(new HttpClient(), NullLoggerFactory.Instance), endpoint: null);
        var projectClient = new AIProjectClient(new Uri(FoundryEndpoint), new DefaultAzureCredential());
        return new AgentFactory(options, projectClient, mcpTools, builders, NullLoggerFactory.Instance);
    }

    [Fact]
    public async Task CreateAsync_Kind_RunsOnlyTheMatchingBuilder()
    {
        var chat = new FakeBuilder(AgentKind.Chat);
        var allocation = new FakeBuilder(AgentKind.Allocation);
        await using var factory = CreateFactory(chat, allocation);

        var (agent, history) = await factory.CreateAsync(AgentKind.Chat, "contexte", "isin");

        Assert.NotNull(agent);
        Assert.NotNull(history);
        Assert.Equal(("contexte", "isin"), Assert.Single(chat.Calls));
        Assert.Empty(allocation.Calls);
    }

    [Fact]
    public async Task CreateAsync_KindWithoutBuilder_ThrowsArgumentOutOfRange()
    {
        await using var factory = CreateFactory(new FakeBuilder(AgentKind.Chat));

        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => factory.CreateAsync(AgentKind.Stock));
    }

    [Fact]
    public void Constructor_TwoBuildersForOneKind_Throws()
    {
        Assert.Throws<ArgumentException>(() => CreateFactory(new FakeBuilder(AgentKind.Chat), new FakeBuilder(AgentKind.Chat)));
    }

    [Theory]
    [InlineData(AgentKind.Chat)]
    [InlineData(AgentKind.Weather)]
    [InlineData(AgentKind.Stock)]
    [InlineData(AgentKind.Allocation)]
    [InlineData(AgentKind.News)]
    [InlineData(AgentKind.Context)]
    public async Task Build_EveryKind_IsWired(AgentKind kind)
    {
        await using var factory = CreateBuilder().Build(Options());

        // Any outcome but "unknown kind" proves a builder is wired: Allocation fails on the missing MCP URL,
        // Weather, Stock and News on the missing input.
        var ex = await Record.ExceptionAsync(() => factory.CreateAsync(kind));

        Assert.IsNotType<ArgumentOutOfRangeException>(ex);
    }

    [Fact]
    public async Task Build_AllocationWithoutMcpUrl_ThrowsInvalidOperation()
    {
        await using var factory = CreateBuilder().Build(Options());

        await Assert.ThrowsAsync<InvalidOperationException>(() => factory.CreateAsync(AgentKind.Allocation));
    }

    [Fact]
    public async Task AddAgentFactoryBuilder_HostServicesRegistered_ResolvesBuilder()
    {
        var services = new ServiceCollection();
        services.AddSingleton<ILoggerFactory>(NullLoggerFactory.Instance);
        services.AddSingleton(new Mock<INewsService>().Object);
        services.AddSingleton(new Mock<IWebSearchService>().Object);
        services.AddInvestZaptoMcp();
        services.AddAgentFactoryBuilder();
        using var provider = services.BuildServiceProvider();

        await using var factory = provider.GetRequiredService<AgentFactoryBuilder>().Build(Options());

        var (agent, _) = await factory.CreateAsync(AgentKind.Weather, "Paris");

        Assert.NotNull(agent);
    }

    private sealed class FakeBuilder(AgentKind kind) : IAgentDefinitionBuilder
    {
        public List<(string? ContextInput, string? OptionalInput)> Calls { get; } = [];

        public AgentKind Kind => kind;

        public Task<AgentDefinition> BuildAsync(string? contextInput, string? optionalInput, CancellationToken cancellationToken = default)
        {
            this.Calls.Add((contextInput, optionalInput));
            return Task.FromResult(new AgentDefinition("instructions", null, $"chat-history-{kind}.json"));
        }
    }
}

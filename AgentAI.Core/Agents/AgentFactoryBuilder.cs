using Azure.AI.Projects;
using Azure.Identity;
using Microsoft.Extensions.Logging;

namespace AgentAI;

/// <summary>
/// Builds <see cref="IAgentFactory"/> instances from the configuration, wiring each agent definition to its services.
/// </summary>
/// <remarks>
/// Separate from the factory because the configuration is not known when the container is built: a host whose
/// settings can change (MAUI reads them from the Api on every run) builds one factory per run, while the services
/// injected here stay shared. Each factory owns its MCP connection, so the caller disposes it after the run.
/// </remarks>
public sealed class AgentFactoryBuilder(
    InvestZaptoMcpClient investZaptoConnector,
    INewsService newsService,
    IWebSearchService webSearchService,
    IWeatherService weatherService,
    TimeProvider timeProvider,
    ILoggerFactory loggerFactory)
{
    public IAgentFactory Build(AgentAIOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        var mcpTools = new InvestZaptoMcpToolSource(investZaptoConnector, options.InvestZaptoMcpUrl);
        
        IAgentDefinitionBuilder[] builders =
        [
            new ChatAgentDefinitionBuilder(),
            new WeatherAgentDefinitionBuilder(weatherService),
            new StockAgentDefinitionBuilder(timeProvider),
            new AllocationAgentDefinitionBuilder(mcpTools, timeProvider),
            new NewsAgentDefinitionBuilder(newsService, timeProvider),
            new ContextAgentDefinitionBuilder(webSearchService),
        ];

        return new AgentFactory(options, CreateProjectClient(options), mcpTools, builders, loggerFactory);
    }

    // Managed identity is excluded: the agents only run on the owner's PC, where it would just slow down the
    // credential chain before reaching the Azure CLI login (az login).
    private static AIProjectClient CreateProjectClient(AgentAIOptions options) =>
        new(new Uri(options.FoundryProjectEndpoint), new DefaultAzureCredential(new DefaultAzureCredentialOptions { ExcludeManagedIdentityCredential = true }));
}

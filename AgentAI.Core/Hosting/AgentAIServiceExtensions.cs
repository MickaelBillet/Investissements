using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;

namespace AgentAI;

/// <summary>
/// Registers the AgentAI services in a host's dependency injection container (e.g. a MAUI app).
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers <see cref="IAgentFactory"/> and its dependencies.
    /// </summary>
    /// <param name="services">The service collection to add to.</param>
    /// <param name="options">Host-provided configuration.</param>
    /// <remarks>
    /// The host must also register logging (<c>AddLogging</c>).
    /// The HTTP clients are typed clients of <see cref="IHttpClientFactory"/> (see
    /// <see cref="InvestZaptoMcpServiceExtensions.AddInvestZaptoMcp"/> and
    /// <see cref="NewsServiceExtensions.AddNewsServices"/>); the host chooses their primary handler through
    /// <c>AddHttpClient&lt;T&gt;()</c>.
    /// Registrations use <c>TryAdd</c> so a host can substitute its own <see cref="INewsService"/> or
    /// <see cref="TimeProvider"/> by registering them beforehand; <see cref="InvestZaptoMcpClient"/> must be
    /// substituted after this call.
    /// </remarks>
    public static IServiceCollection AddAgentAI(this IServiceCollection services, AgentAIOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        return services.AddAgentAI(_ => options);
    }

    /// <summary>
    /// Registers <see cref="IAgentFactory"/> and its dependencies, reading the configuration lazily.
    /// </summary>
    /// <param name="services">The service collection to add to.</param>
    /// <param name="optionsFactory">Builds the host configuration the first time it is needed.</param>
    /// <remarks>
    /// Lets a host whose settings are user-editable (e.g. MAUI) start without them: a missing value then
    /// surfaces when an agent is first used, where it can be reported, instead of breaking application start-up.
    /// See <see cref="AddAgentAI(IServiceCollection, AgentAIOptions)"/> for the other host requirements.
    /// </remarks>
    public static IServiceCollection AddAgentAI(this IServiceCollection services, Func<IServiceProvider, AgentAIOptions> optionsFactory)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(optionsFactory);

        services.TryAddSingleton(optionsFactory);
        services.TryAddSingleton(TimeProvider.System);
        services.AddInvestZaptoMcp();
        services.AddNewsServices();

        // Singleton: the factory owns the shared MCP connection, and the container disposes it on shutdown.
        // Built explicitly because AgentFactory has two constructors and IWeatherService is not registered.
        services.TryAddSingleton<IAgentFactory>(sp => new AgentFactory(
            sp.GetRequiredService<AgentAIOptions>(),
            sp.GetRequiredService<InvestZaptoMcpClient>(),
            sp.GetRequiredService<INewsService>(),
            sp.GetRequiredService<ILoggerFactory>()));

        return services;
    }
}

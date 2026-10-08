using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

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
    /// The host must also register an <see cref="HttpClient"/> and logging (<c>AddLogging</c>).
    /// The <see cref="HttpClient"/> is deliberately not registered here: the host owns the handler
    /// pipeline, and the InvestZapto backend needs <see cref="ForceContentLengthHandler"/> in front of a
    /// <see cref="SocketsHttpHandler"/> to avoid chunked POSTs.
    /// Registrations use <c>TryAdd</c> so a host can substitute its own <see cref="INewsService"/>,
    /// <see cref="TimeProvider"/> or <see cref="InvestZaptoMcpClient"/> by registering them beforehand.
    /// </remarks>
    public static IServiceCollection AddAgentAI(this IServiceCollection services, AgentAIOptions options)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(options);

        services.TryAddSingleton(options);
        services.TryAddSingleton(TimeProvider.System);
        services.TryAddSingleton<InvestZaptoMcpClient>();
        services.TryAddSingleton<INewsService, RssNewsService>();

        // Singleton: the factory owns the shared MCP connection, and the container disposes it on shutdown.
        // Built explicitly because AgentFactory has two constructors and IWeatherService is not registered.
        services.TryAddSingleton<IAgentFactory>(sp => new AgentFactory(
            sp.GetRequiredService<AgentAIOptions>(),
            sp.GetRequiredService<InvestZaptoMcpClient>(),
            sp.GetRequiredService<INewsService>(),
            sp.GetRequiredService<Microsoft.Extensions.Logging.ILoggerFactory>()));

        return services;
    }
}

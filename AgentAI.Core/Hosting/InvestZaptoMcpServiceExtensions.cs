using Microsoft.Extensions.DependencyInjection;

namespace AgentAI;

/// <summary>
/// Registers the client of the InvestZapto MCP server.
/// </summary>
public static class InvestZaptoMcpServiceExtensions
{
    /// <summary>
    /// Registers <see cref="InvestZaptoMcpClient"/> as a typed client of <see cref="IHttpClientFactory"/>.
    /// </summary>
    /// <param name="services">The service collection to add to.</param>
    /// <returns>
    /// The client builder, so the host can choose the primary handler (e.g. a <see cref="SocketsHttpHandler"/>,
    /// which a browser host does not have) and the other client options.
    /// </returns>
    /// <remarks>
    /// <see cref="ForceContentLengthHandler"/> is part of the pipeline: the InvestZapto backend answers every
    /// chunked POST with a <c>500 Backend call failure</c>, and the handler is platform-neutral.
    /// Unlike the other registrations of this library this one is not a <c>TryAdd</c>: a host that substitutes
    /// <see cref="InvestZaptoMcpClient"/> must register its own after this call.
    /// </remarks>
    public static IHttpClientBuilder AddInvestZaptoMcp(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        return services
            .AddHttpClient<InvestZaptoMcpClient>()
            .AddHttpMessageHandler(() => new ForceContentLengthHandler());
    }
}

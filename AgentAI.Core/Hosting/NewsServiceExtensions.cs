using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace AgentAI;

/// <summary>
/// Registers the company-news feature (<see cref="INewsService"/> and its feed sources).
/// </summary>
public static class NewsServiceExtensions
{
    /// <summary>
    /// Registers <see cref="INewsService"/>, the built-in <see cref="INewsFeedSource"/>s and their dependencies.
    /// </summary>
    /// <param name="services">The service collection to add to.</param>
    /// <remarks>
    /// The feed reader and the ticker resolver are typed clients of <see cref="IHttpClientFactory"/>: each has its
    /// own pipeline, so headers added to another client of the host (e.g. a dashboard password) never reach the
    /// third-party feeds. A host tunes them with <c>AddHttpClient&lt;IRssFeedReader, RssFeedReader&gt;()</c> and
    /// <c>AddHttpClient&lt;YahooTickerResolver&gt;()</c> (primary handler, timeout...).
    /// The host adds a source by registering its own <see cref="INewsFeedSource"/>: <see cref="NewsService"/>
    /// receives every registered source. <see cref="INewsService"/> and the sources use <c>TryAdd</c> so a host can
    /// substitute them by registering beforehand. The host must also register logging.
    /// </remarks>
    public static IServiceCollection AddNewsServices(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.TryAddSingleton(TimeProvider.System);

        services.AddHttpClient<IRssFeedReader, RssFeedReader>();
        services.AddHttpClient<YahooTickerResolver>();

        services.TryAddEnumerable(ServiceDescriptor.Singleton<INewsFeedSource, GoogleNewsFeedSource>());
        services.TryAddEnumerable(ServiceDescriptor.Singleton<INewsFeedSource, YahooFinanceFeedSource>());

        services.TryAddSingleton<INewsService, NewsService>();

        return services;
    }
}

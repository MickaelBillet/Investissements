using System.ComponentModel;
using InvestissementsDashboard.Shared.Models;
using Microsoft.Extensions.AI;

namespace AgentAI;

/// <summary>
/// Exposes <see cref="IWebSearchService"/> to an agent as the <c>SearchWeb</c> tool, with a cap on the number of searches.
/// </summary>
/// <remarks>
/// One instance per agent: the cap protects the provider's free monthly quota from a model that keeps searching,
/// and is counted over the agent's whole run.
/// </remarks>
internal sealed class WebSearchTool(IWebSearchService service, int maxSearches)
{
    internal const string Name = "SearchWeb";

    internal const string LimitReachedMessage = "Plafond de recherches atteint : rédige ta réponse avec les informations déjà obtenues et indique ce qui n'a pas pu être vérifié.";

    private int _searchCount;

    public AIFunction AsAIFunction() =>
        AIFunctionFactory.Create(
            this.SearchAsync,
            name: Name,
            description: $"Recherche sur le web et renvoie les pages trouvées (titre, lien, extrait, date de publication). Au plus {maxSearches} recherches.");

    /// <returns>The pages found, or <see cref="LimitReachedMessage"/> once the cap is reached.</returns>
    internal async Task<object> SearchAsync(
        [Description("Requête précise, sur un seul sujet.")] string query,
        [Description("Type de sources à privilégier : General, News (actualités) ou Finance (marchés).")] WebSearchTopic topic,
        [Description("Ancienneté maximale des pages : Day, Week, Month, Year, ou Any pour ne pas filtrer.")] WebSearchTimeRange timeRange,
        CancellationToken cancellationToken)
    {
        if (Interlocked.Increment(ref _searchCount) > maxSearches)
        {
            return LimitReachedMessage;
        }

        return await service.SearchAsync(query, topic, timeRange, cancellationToken).ConfigureAwait(false);
    }
}

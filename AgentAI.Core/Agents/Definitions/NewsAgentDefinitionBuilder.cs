using System.ComponentModel;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;

namespace AgentAI;

internal sealed class NewsAgentDefinitionBuilder(INewsService newsService, TimeProvider timeProvider) : IAgentDefinitionBuilder
{
    internal const int LookbackDays = 7;

    internal const string ToolName = "GetLatestNews";

    public AgentKind Kind => AgentKind.News;

    /// <param name="optionalInput">The ISIN, if known.</param>
    public Task<AgentDefinition> BuildAsync(string? contextInput, string? optionalInput, CancellationToken cancellationToken = default)
    {
        var company = AgentInput.Require(contextInput, "un nom de société");

        return Task.FromResult(new AgentDefinition(
            Instructions: InstructionLoader.Load("ActualitesSociete.md"),
            Tools: [this.CreateNewsTool()],
            HistoryFileName: "chat-history-news.json",
            AIContextProviders: new CompositeContextProvider(new AIContextProvider[]
            {
                new NewsContextProvider(company, optionalInput, LookbackDays, timeProvider),
            })));
    }

    private AITool CreateNewsTool()
    {
        // The look-back window is fixed here rather than exposed to the model, so the agent cannot
        // silently widen it beyond what the context provider announces.
        return AIFunctionFactory.Create(
            async (
                [Description("Nom de la société à rechercher.")] string company,
                [Description("ISIN de la société, ou vide s'il n'est pas connu.")] string? isin,
                CancellationToken cancellationToken) =>
                    await newsService.GetLatestNewsAsync(company, isin, LookbackDays, cancellationToken).ConfigureAwait(false),
            name: ToolName,
            description: "Récupère les actualités publiées sur une société durant la dernière semaine (titre, source, date, lien), de la plus récente à la plus ancienne.");
    }
}

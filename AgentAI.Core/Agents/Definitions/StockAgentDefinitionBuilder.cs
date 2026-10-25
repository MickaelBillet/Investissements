using Microsoft.Agents.AI;

namespace AgentAI;

internal sealed class StockAgentDefinitionBuilder(TimeProvider timeProvider) : IAgentDefinitionBuilder
{
    public AgentKind Kind => AgentKind.Stock;

    /// <param name="optionalInput">The ISIN, if known.</param>
    public Task<AgentDefinition> BuildAsync(string? contextInput, string? optionalInput, CancellationToken cancellationToken = default)
    {
        var stockName = AgentInput.Require(contextInput, "un nom d'action");

        return Task.FromResult(new AgentDefinition(
            Instructions: InstructionLoader.Load("AnalyseAction.md"),
            Tools: null,
            HistoryFileName: "chat-history-stock.json",
            AIContextProviders: new CompositeContextProvider(new AIContextProvider[]
            {
                new DateContextProvider(timeProvider),
                new StockContextProvider(stockName, optionalInput),
            })));
    }
}

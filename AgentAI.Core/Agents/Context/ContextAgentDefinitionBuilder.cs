using OpenAI.Responses;

namespace AgentAI;

#pragma warning disable OPENAI001 // OpenAI.Responses types are for evaluation purposes only and subject to change.

internal sealed class ContextAgentDefinitionBuilder(IWebSearchService webSearchService) : IAgentDefinitionBuilder
{
    // About a dozen subjects to cover (Context.md) with some room for a follow-up search, within the provider's
    // free quota of 1,000 searches a month.
    internal const int MaxWebSearches = 15;

    public AgentKind Kind => AgentKind.Context;

    // No DateContextProvider: its "older than 6 months" rule contradicts the 30-day window of Context.md,
    // and the analysis date comes with the message.
    public Task<AgentDefinition> BuildAsync(string? contextInput, string? optionalInput, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(new AgentDefinition(
            Instructions: InstructionLoader.Load("Context.md"),
            // A new tool per agent, so that the cap on searches applies to one run and not to the builder's lifetime.
            Tools: [new WebSearchTool(webSearchService, MaxWebSearches).AsAIFunction()],
            HistoryFileName: "chat-history-context.json",
            ReasoningEffort: ResponseReasoningEffortLevel.Low,
            KeepHistory: false));
    }
}

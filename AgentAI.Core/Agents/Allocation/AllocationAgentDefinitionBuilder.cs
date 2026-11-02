using Microsoft.Agents.AI;
using OpenAI.Responses;

namespace AgentAI;

#pragma warning disable OPENAI001 // OpenAI.Responses types are for evaluation purposes only and subject to change.

/// <summary>
/// The portfolio report agent: its tools are those of the InvestZapto MCP server.
/// </summary>
internal sealed class AllocationAgentDefinitionBuilder(InvestZaptoMcpToolSource mcpTools, TimeProvider timeProvider) : IAgentDefinitionBuilder
{
    public AgentKind Kind => AgentKind.Allocation;

    public async Task<AgentDefinition> BuildAsync(string? contextInput, string? optionalInput, CancellationToken cancellationToken = default)
    {
        return new AgentDefinition(
            Instructions: InstructionLoader.Load("AllocationPortefeuille.md"),
            Tools: await mcpTools.GetToolsAsync(cancellationToken).ConfigureAwait(false),
            HistoryFileName: "chat-history-allocation.json",
            // Dated because the report is.
            AIContextProviders: new CompositeContextProvider(new AIContextProvider[] { new DateContextProvider(timeProvider) }),
            ReasoningEffort: ResponseReasoningEffortLevel.Low,
            KeepHistory: false);
    }
}

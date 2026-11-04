using AgentAI.Core.Agents.Definitions;
using OpenAI.Responses;

namespace AgentAI.Core.Agents.Synthesis;

#pragma warning disable OPENAI001 // OpenAI.Responses types are for evaluation purposes only and subject to change.

internal sealed class SynthesisAgentDefinitionBuilder : IAgentDefinitionBuilder
{
    public AgentKind Kind => AgentKind.Synthesis;

    public Task<AgentDefinition> BuildAsync(string? contextInput, string? optionalInput, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(new AgentDefinition(
            // Kept apart so the investor can update the profile without touching the method.
            Instructions: $"{InstructionLoader.Load("Synthesis.md")}\n\n{InstructionLoader.Load("InvestorProfile.md")}",
            Tools: null,                                       // its inputs arrive in the message and the instructions
            HistoryFileName: "chat-history-synthesis.json",
            ReasoningEffort: ResponseReasoningEffortLevel.Low,  // to be tuned once durations are measured
            KeepHistory: false));                              // machine-read JSON answer
    }
}
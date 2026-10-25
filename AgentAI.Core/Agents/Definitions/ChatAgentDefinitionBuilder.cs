namespace AgentAI;

internal sealed class ChatAgentDefinitionBuilder : IAgentDefinitionBuilder
{
    public AgentKind Kind => AgentKind.Chat;

    public Task<AgentDefinition> BuildAsync(string? contextInput, string? optionalInput, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(new AgentDefinition(
            Instructions: "You are a helpful assistant.",
            Tools: null,
            HistoryFileName: "chat-history-chat.json"));
    }
}

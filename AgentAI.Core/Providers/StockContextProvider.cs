using Microsoft.Agents.AI;

namespace AgentAI;

/// <summary>
/// Injects the name of the stock to analyze into the agent's context, so the user only has to type
/// the stock name rather than a full free-text question.
/// </summary>
public sealed class StockContextProvider(string stockName) : AIContextProvider
{
    protected override ValueTask<AIContext> ProvideAIContextAsync(
        InvokingContext context, CancellationToken ct = default)
    {
        return ValueTask.FromResult(new AIContext
        {
            Instructions = $"Action à analyser : {stockName}.",
        });
    }
}

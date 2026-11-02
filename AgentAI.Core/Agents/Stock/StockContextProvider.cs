using Microsoft.Agents.AI;

namespace AgentAI;

/// <summary>
/// Injects the name of the stock to analyze into the agent's context, so the user only has to type
/// the stock name rather than a full free-text question. The ISIN, when known, removes any ambiguity
/// between securities sharing a name (share classes, listings).
/// </summary>
public sealed class StockContextProvider(string stockName, string? isin = null) : AIContextProvider
{
    protected override ValueTask<AIContext> ProvideAIContextAsync(
        InvokingContext context, CancellationToken ct = default)
    {
        var instructions = string.IsNullOrWhiteSpace(isin)
            ? $"Action à analyser : {stockName}."
            : $"Action à analyser : {stockName} (ISIN : {isin.Trim()}).";

        return ValueTask.FromResult(new AIContext
        {
            Instructions = instructions,
        });
    }
}

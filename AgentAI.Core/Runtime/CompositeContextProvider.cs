using Microsoft.Agents.AI;

namespace AgentAI;

public sealed class CompositeContextProvider(IEnumerable<AIContextProvider> providers) : AIContextProvider
{
    protected override ValueTask<AIContext> ProvideAIContextAsync(InvokingContext ctx, CancellationToken ct = default)
    {
        return ProvideCombinedAIContextAsync(ctx, ct);
    }

    private async ValueTask<AIContext> ProvideCombinedAIContextAsync(InvokingContext ctx, CancellationToken ct)
    {
        var contexts = new List<AIContext>();
        foreach (var p in providers)
            contexts.Add(await p.InvokingAsync(ctx, ct));

        return new AIContext
        {
            Instructions = string.Join("\n\n", contexts.Select(c => c.Instructions).Where(i => !string.IsNullOrWhiteSpace(i))),
            Messages = contexts.SelectMany(c => c.Messages ?? []).ToList(),
            Tools = contexts.SelectMany(c => c.Tools ?? []).ToList()
        };
    }
}

using System.Globalization;
using Microsoft.Agents.AI;

namespace AgentAI;

public sealed class DateContextProvider(TimeProvider time) : AIContextProvider
{
    protected override ValueTask<AIContext> ProvideAIContextAsync(InvokingContext context, CancellationToken ct = default)
    {
        var today = time.GetLocalNow().ToString("dddd d MMMM yyyy", new CultureInfo("fr-FR"));

        return ValueTask.FromResult(new AIContext
        {
            Instructions = $"""
                Nous sommes le {today}.
                Toute actualité antérieure à 6 mois doit être signalée comme ancienne.
                """
        });
    }
}

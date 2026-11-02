using System.Globalization;
using Microsoft.Agents.AI;

namespace AgentAI;

/// <summary>
/// Injects the company (and optional ISIN) to look up, plus today's date and the look-back window,
/// so the user only has to type the company rather than a full free-text question.
/// </summary>
/// <remarks>
/// The date lives here rather than in <see cref="DateContextProvider"/>: that provider tells the agent
/// to flag anything older than 6 months, which contradicts the one-week window of this agent.
/// </remarks>
public sealed class NewsContextProvider(string company, string? isin, int days, TimeProvider time) : AIContextProvider
{
    protected override ValueTask<AIContext> ProvideAIContextAsync(
        InvokingContext context, CancellationToken ct = default)
    {
        var today = time.GetLocalNow().ToString("dddd d MMMM yyyy", new CultureInfo("fr-FR"));
        var isinLine = string.IsNullOrWhiteSpace(isin)
            ? "ISIN : non fourni."
            : $"ISIN : {isin.Trim()}.";

        return ValueTask.FromResult(new AIContext
        {
            Instructions = $"""
                Nous sommes le {today}.
                Société à suivre : {company}.
                {isinLine}
                Fenêtre de recherche : les {days} derniers jours.
                """,
        });
    }
}

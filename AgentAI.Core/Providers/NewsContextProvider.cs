using System.Globalization;
using Microsoft.Agents.AI;

namespace AgentAI;

/// <summary>
/// Injects the company (and optional ticker) to look up, plus today's date and the look-back window,
/// so the user only has to type the company rather than a full free-text question.
/// </summary>
/// <remarks>
/// The date lives here rather than in <see cref="DateContextProvider"/>: that provider tells the agent
/// to flag anything older than 6 months, which contradicts the one-week window of this agent.
/// </remarks>
public sealed class NewsContextProvider(string company, string? ticker, int days, TimeProvider time) : AIContextProvider
{
    protected override ValueTask<AIContext> ProvideAIContextAsync(
        InvokingContext context, CancellationToken ct = default)
    {
        var today = time.GetLocalNow().ToString("dddd d MMMM yyyy", new CultureInfo("fr-FR"));
        var tickerLine = string.IsNullOrWhiteSpace(ticker)
            ? "Ticker : non fourni."
            : $"Ticker : {ticker}.";

        return ValueTask.FromResult(new AIContext
        {
            Instructions = $"""
                Nous sommes le {today}.
                Société à suivre : {company}.
                {tickerLine}
                Fenêtre de recherche : les {days} derniers jours.
                """,
        });
    }
}

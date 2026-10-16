using System.Net;
using AgentAI;
using InvestissementsDashboard.Client.Services;
using Microsoft.Agents.AI;
using Microsoft.Extensions.Logging;

namespace InvestissementsDashboard.Maui.Services;

/// <summary>
/// Runs an <see cref="AgentChoice"/> against Azure AI Foundry through <see cref="IAgentFactory"/>.
/// </summary>
/// <remarks>
/// A factory is built for every run (instead of a singleton) so that a change made in the settings page
/// is picked up immediately, without restarting the application.
/// </remarks>
public sealed class AgentRunner(
    AgentOptionsProvider optionsProvider,
    Func<AgentAIOptions, IAgentFactory> factoryCreator,
    ILogger<AgentRunner> logger,
    TimeSpan? timeout = null) : IAgentRunner
{
    // Without a bound a stalled connection keeps the dialog spinning forever; the report agents (several tool
    // calls) are slower than the single-shot ones, so the limit is generous.
    internal static readonly TimeSpan DefaultTimeout = TimeSpan.FromMinutes(3);

    public async Task<string> RunAsync(AgentChoice agent, string? assetName, string? isin, CancellationToken cancellationToken = default)
    {
        var options = optionsProvider.Create();
        AgentKind kind = GetAgentKind(agent);
        string triggerMessage = GetMessage(agent);

        string? optionalInput = string.IsNullOrWhiteSpace(isin) ? null : isin.Trim();

        using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeoutCts.CancelAfter(timeout ?? DefaultTimeout);
        var token = timeoutCts.Token;

        await using var agentFactory = factoryCreator(options);
        logger.LogInformation("Agent {Agent}: starting (asset: {Asset}, model: {Model})", agent, assetName ?? "-", options.Model);
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();

        // Naming the stage in the timeout message tells whether the MCP connection or the model call stalled.
        const string creating = "creating the agent (MCP connection)";
        const string running = "waiting for the model's answer";
        string stage = creating;

        (AIAgent aiAgent, ICustomChatHistoryProvider historyProvider) created;
        try
        {
            // CreateAsync takes no token: WaitAsync lets the timeout and the user's cancellation interrupt the wait.
            created = await agentFactory.CreateAsync(kind, assetName, optionalInput).WaitAsync(token);
        }
        catch (HttpRequestException ex) when (ex.StatusCode == HttpStatusCode.Unauthorized)
        {
            // The MCP server is the only endpoint an agent connects to at creation time.
            throw new InvalidOperationException("The MCP key is missing or rejected: set it in the settings page.", ex);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            throw TimedOut(agent, stage);
        }

        var (aiAgent, historyProvider) = created;
        stage = running;
        logger.LogInformation("Agent {Agent}: created after {Seconds:0.0}s, calling the model", agent, stopwatch.Elapsed.TotalSeconds);

        try
        {
            var session = await aiAgent.CreateSessionAsync(token);
            var response = await aiAgent.RunAsync(triggerMessage, session, cancellationToken: token);
            logger.LogInformation("Agent {Agent}: answered after {Seconds:0.0}s ({Chars} characters)", agent, stopwatch.Elapsed.TotalSeconds, response.Text.Length);
            return response.Text;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            // The user closed the dialog: the persisted history is still valid, so keep it.
            throw;
        }
        catch (Exception ex)
        {
            // A failed call may leave a payload the API rejects on every later run (e.g. after a package update).
            logger.LogError(ex, "Agent {Agent} failed for {Asset}; resetting its history", agent, assetName);
            historyProvider.ResetWithBackup($"agent {agent} failed");

            if (ex is OperationCanceledException)
            {
                throw TimedOut(agent, stage);
            }

            throw;
        }
    }

    private TimeoutException TimedOut(AgentChoice agent, string stage)
    {
        var limit = timeout ?? DefaultTimeout;
        logger.LogError("Agent {Agent} timed out after {Limit} while {Stage}", agent, limit, stage);
        return new TimeoutException($"The agent did not answer within {limit.TotalMinutes:0.#} min (stalled while {stage}).");
    }

    private string GetMessage(AgentChoice agent)
    {
        return agent switch
        {
            AgentChoice.Stock => "Lance l'analyse selon la grille en vigueur.",
            AgentChoice.News => "Donne-moi les dernières actualités de cette société.",
            AgentChoice.Allocation => "Dresse l'état complet de mon portefeuille : positions, valeurs et variations.",
            _ => throw new ArgumentOutOfRangeException(nameof(agent), agent, "Unknown agent."),
        };
    }


    private AgentKind GetAgentKind(AgentChoice agent)
    {
        return agent switch
        {
            AgentChoice.Stock => AgentKind.Stock,
            AgentChoice.News => AgentKind.News,
            AgentChoice.Allocation => AgentKind.Allocation,
            _ => throw new ArgumentOutOfRangeException(nameof(agent), agent, "Unknown agent."),
        };
    }

}

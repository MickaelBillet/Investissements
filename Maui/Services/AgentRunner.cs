using AgentAI;
using InvestissementsDashboard.Client.Services;
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
    ILogger<AgentRunner> logger) : IAgentRunner
{
    public async Task<string> RunAsync(AgentChoice agent, string assetName, CancellationToken cancellationToken = default)
    {
        var options = optionsProvider.Create();
        AgentKind kind = GetAgentKind(agent);
        string triggerMessage = GetMessage(agent);

        await using var agentFactory = factoryCreator(options);
        var (aiAgent, historyProvider) = await agentFactory.CreateAsync(kind, assetName);

        try
        {
            var session = await aiAgent.CreateSessionAsync(cancellationToken);
            var response = await aiAgent.RunAsync(triggerMessage, session, cancellationToken: cancellationToken);
            return response.Text;
        }
        catch (OperationCanceledException)
        {
            // The user closed the dialog: the persisted history is still valid, so keep it.
            throw;
        }
        catch (Exception ex)
        {
            // A failed call may leave a payload the API rejects on every later run (e.g. after a package update).
            logger.LogError(ex, "Agent {Agent} failed for {Asset}; resetting its history", agent, assetName);
            historyProvider.ResetWithBackup($"agent {agent} failed");
            throw;
        }
    }

    private string GetMessage(AgentChoice agent)
    {
        return agent switch
        {
            AgentChoice.Stock => "Lance l'analyse selon la grille en vigueur.",
            AgentChoice.News => "Donne-moi les dernières actualités de cette société.",
            _ => throw new ArgumentOutOfRangeException(nameof(agent), agent, "Unknown agent."),
        };
    }


    private AgentKind GetAgentKind(AgentChoice agent)
    {
        return agent switch
        {
            AgentChoice.Stock => AgentKind.Stock,
            AgentChoice.News => AgentKind.News,
            _ => throw new ArgumentOutOfRangeException(nameof(agent), agent, "Unknown agent."),
        };
    }

}

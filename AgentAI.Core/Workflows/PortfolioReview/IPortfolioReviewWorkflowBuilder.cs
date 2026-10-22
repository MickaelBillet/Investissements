using Microsoft.Agents.AI.Workflows;

namespace AgentAI;

/// <summary>
/// Assembles the portfolio review workflow from the agents built by an <see cref="IAgentFactory"/>.
/// </summary>
/// <remarks>
/// The factory and the distribution source are parameters rather than constructor dependencies: hosts create them
/// per run (so a settings change applies immediately, and the source's Api client is not held for the whole
/// application lifetime) while the builder itself is stateless and can be a singleton.
/// </remarks>
public interface IPortfolioReviewWorkflowBuilder
{
    /// <param name="agentFactory">The factory of the current run; it must outlive the workflow's execution.</param>
    /// <param name="distributionSource">Supplies the geographic and sector distributions read by the distribution step.</param>
    /// <param name="cancellationToken">Interrupts the agents' creation (MCP connection), which takes no token itself.</param>
    Task<Workflow> BuildAsync(IAgentFactory agentFactory, IAssetDistributionSource distributionSource, CancellationToken cancellationToken = default);
}

namespace AgentAI;

/// <summary>
/// Assembles the portfolio review workflow from the agents built by <see cref="AgentFactory"/>.
/// </summary>
internal static class PortfolioReviewWorkflow
{
    /// <summary>
    /// Builds the allocation step on the existing Allocation agent, whose instructions already produce
    /// an <see cref="InvestissementsDashboard.Shared.Models.AllocationResult"/> JSON.
    /// </summary>
    public static async Task<AllocationExecutor> CreateAllocationExecutorAsync(AgentFactory factory)
    {
        ArgumentNullException.ThrowIfNull(factory);

        // The history provider is not needed here: the Allocation agent does not replay its history.
        var (agent, _) = await factory.CreateAsync(AgentKind.Allocation).ConfigureAwait(false);
        return new AllocationExecutor(agent);
    }
}

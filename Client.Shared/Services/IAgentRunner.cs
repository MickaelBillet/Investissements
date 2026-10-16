namespace InvestissementsDashboard.Client.Services;

public enum AgentChoice
{
    Stock,
    News,

    /// <summary>Portfolio-wide report: not tied to an asset, so no asset name or ISIN is passed.</summary>
    Allocation
}

/// <summary>
/// Runs an AI agent on a given asset, or on the whole portfolio for <see cref="AgentChoice.Allocation"/>. Deliberately not registered by <c>AddInvestissementsClient</c>:
/// only the MAUI host provides an implementation, so the WASM site shows no agent entry point.
/// </summary>
public interface IAgentRunner
{
    /// <param name="assetName">The asset to analyse; <see langword="null"/> for the agents that work on the whole portfolio.</param>
    /// <param name="isin">
    /// The asset's ISIN, when known; lets the agent disambiguate between securities sharing a name.
    /// Blank is treated as unknown.
    /// </param>
    Task<string> RunAsync(AgentChoice agent, string? assetName, string? isin, CancellationToken cancellationToken = default);
}

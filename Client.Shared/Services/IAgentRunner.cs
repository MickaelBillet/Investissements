namespace InvestissementsDashboard.Client.Services;

public enum AgentChoice
{
    Stock,
    News
}

/// <summary>
/// Runs an AI agent on a given asset. Deliberately not registered by <c>AddInvestissementsClient</c>:
/// only the MAUI host provides an implementation, so the WASM site shows no agent entry point.
/// </summary>
public interface IAgentRunner
{
    Task<string> RunAsync(AgentChoice agent, string assetName, CancellationToken cancellationToken = default);
}

// Copyright (c) Microsoft. All rights reserved.

using Microsoft.Agents.AI;

namespace AgentAI;

/// <summary>
/// Builds an <see cref="AIAgent"/> for a given <see cref="AgentKind"/>.
/// </summary>
public interface IAgentFactory : IAsyncDisposable
{
    /// <summary>
    /// Creates the <see cref="AIAgent"/> configured for <paramref name="kind"/>, along with the
    /// history provider backing it so the caller can reset it if the persisted history turns out
    /// to be incompatible (e.g. after a NuGet update).
    /// </summary>
    /// <param name="kind">Which agent configuration to build.</param>
    /// <param name="contextInput">
    /// The single piece of user-provided data the agent's context provider needs (e.g. a city for
    /// <see cref="AgentKind.Weather"/>, a stock name for <see cref="AgentKind.Stock"/>). Ignored by
    /// kinds that don't use a context provider.
    /// </param>
    /// <param name="optionalInput">
    /// A second, optional piece of user-provided data for kinds that take one (the ticker for
    /// <see cref="AgentKind.News"/>). Ignored by the other kinds.
    /// </param>
    /// <remarks>
    /// Asynchronous because some agent kinds (e.g. <see cref="AgentKind.Portfolio"/>) require
    /// connecting to an external MCP server before their tools are known.
    /// </remarks>
    Task<(AIAgent Agent, ICustomChatHistoryProvider HistoryProvider)> CreateAsync(AgentKind kind, string? contextInput = null, string? optionalInput = null);
}

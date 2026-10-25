namespace AgentAI;

/// <summary>
/// Builds the <see cref="AgentDefinition"/> of one <see cref="AgentKind"/>.
/// </summary>
/// <remarks>
/// One implementation per agent, so that adding an agent adds a class instead of growing <see cref="AgentFactory"/>,
/// and each agent only receives the services it uses.
/// </remarks>
internal interface IAgentDefinitionBuilder
{
    AgentKind Kind { get; }

    /// <param name="contextInput">See <see cref="IAgentFactory.CreateAsync"/>.</param>
    /// <param name="optionalInput">See <see cref="IAgentFactory.CreateAsync"/>.</param>
    Task<AgentDefinition> BuildAsync(string? contextInput, string? optionalInput, CancellationToken cancellationToken = default);
}

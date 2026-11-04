// Copyright (c) Microsoft. All rights reserved.


namespace AgentAI.Core.Agents;

/// <summary>
/// Identifies which agent configuration <see cref="AgentFactory"/> should build.
/// </summary>
public enum AgentKind
{
    Chat,
    Weather,
    Stock,
    News,
    Context,
    Synthesis
}

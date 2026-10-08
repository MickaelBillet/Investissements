// Copyright (c) Microsoft. All rights reserved.

using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;

namespace AgentAI;

/// <summary>
/// Static configuration for one agent kind: its instructions, tools, and where its
/// conversation history is persisted.
/// </summary>
/// <param name="Instructions">The system prompt for the agent.</param>
/// <param name="Tools">Tools exposed to the agent, or <see langword="null"/> if none.</param>
/// <param name="HistoryFileName">File name (relative to the app base directory) used to persist chat history.</param>
internal sealed record AgentDefinition(string Instructions, IList<AITool>? Tools, string HistoryFileName, CompositeContextProvider? AIContextProviders = null);

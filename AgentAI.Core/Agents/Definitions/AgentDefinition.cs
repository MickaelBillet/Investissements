// Copyright (c) Microsoft. All rights reserved.

using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using OpenAI.Responses;

namespace AgentAI;

/// <summary>
/// Static configuration for one agent kind: its instructions, tools, and where its
/// conversation history is persisted.
/// </summary>
/// <param name="Instructions">The system prompt for the agent.</param>
/// <param name="Tools">Tools exposed to the agent, or <see langword="null"/> if none.</param>
/// <param name="HistoryFileName">File name (relative to the app base directory) used to persist chat history.</param>
/// <param name="AIContextProviders">Context providers run before each invocation, or <see langword="null"/> if none.</param>
/// <param name="ReasoningEffort">
/// Reasoning effort requested from the model, or <see langword="null"/> for the deployment's default. Lowered for
/// reports that only restate tool data: at the default effort the Allocation report spent ~14,000 reasoning tokens
/// (158 s), past the 100 s network timeout of the Foundry client, which then retried the call.
/// </param>
/// <param name="KeepHistory">
/// <see langword="false"/> for agents whose answer is machine-read (JSON): replaying earlier answers would only
/// grow the prompt and pull the model towards past outputs. The file then holds the last run only, for diagnosis.
/// </param>
#pragma warning disable OPENAI001 // OpenAI.Responses types are for evaluation purposes only and subject to change.
internal sealed record AgentDefinition(string Instructions, IList<AITool>? Tools, string HistoryFileName, CompositeContextProvider? AIContextProviders = null, ResponseReasoningEffortLevel? ReasoningEffort = null, bool KeepHistory = true);

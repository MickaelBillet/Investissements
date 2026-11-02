// Copyright (c) Microsoft. All rights reserved.

using Microsoft.Extensions.AI;

namespace AgentAI;

/// <summary>
/// Exposes read access to the chat messages persisted by a custom chat history provider.
/// </summary>
public interface ICustomChatHistoryProvider
{
    /// <summary>
    /// Gets the chat messages currently persisted for the conversation.
    /// </summary>
    IReadOnlyList<ChatMessage> GetMessages();

    /// <summary>
    /// Clears the persisted history, backing up the previous file first if it exists.
    /// Used when the on-disk history is corrupted or no longer compatible with the current
    /// package versions (e.g. after a NuGet update changes the message serialization format).
    /// </summary>
    /// <param name="reason">Human-readable reason, logged alongside the backup path.</param>
    void ResetWithBackup(string reason);
}

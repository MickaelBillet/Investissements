// Copyright (c) Microsoft. All rights reserved.

using System.Text.Json;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;

namespace AgentAI;

/// <summary>
/// A <see cref="ChatHistoryProvider"/> that persists the conversation history to a local JSON file,
/// so history survives across process restarts.
/// </summary>
internal sealed class CustomChatHistoryProvider : ChatHistoryProvider, ICustomChatHistoryProvider
{
    private readonly string _filePath;
    private readonly List<ChatMessage> _messages;
    private readonly ILogger<CustomChatHistoryProvider> _logger;

    public CustomChatHistoryProvider(string filePath, ILogger<CustomChatHistoryProvider> logger)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);
        ArgumentNullException.ThrowIfNull(logger);

        // Assigned before loading: LoadFromDisk logs when it has to reset a corrupted file.
        this._logger = logger;
        this._filePath = filePath;
        this._messages = this.LoadFromDisk(filePath);
    }

    public IReadOnlyList<ChatMessage> GetMessages() => this._messages;

    public void ResetWithBackup(string reason)
    {
        this.BackupFile(this._filePath, reason);
        this._messages.Clear();
        this.SaveToDiskAsync(CancellationToken.None).GetAwaiter().GetResult();
    }

    protected override ValueTask<IEnumerable<ChatMessage>> ProvideChatHistoryAsync(InvokingContext context, CancellationToken cancellationToken = default)
        => new(this._messages.AsEnumerable());

    protected override async ValueTask StoreChatHistoryAsync(InvokedContext context, CancellationToken cancellationToken = default)
    {
        this._messages.AddRange(this.FilterStripReasoningContent(context.RequestMessages));
        this._messages.AddRange(this.FilterStripReasoningContent(context.ResponseMessages ?? []));

        await this.SaveToDiskAsync(cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Drops <see cref="TextReasoningContent"/> from persisted messages.
    /// </summary>
    /// <remarks>
    /// Reasoning content (e.g. the opaque <see cref="TextReasoningContent.ProtectedData"/> blob returned
    /// by reasoning models) is only valid to replay within the same response chain
    /// (<c>previous_response_id</c>). This provider persists history as plain messages for a fresh
    /// request on every run with server-side storage disabled, so replaying a saved reasoning blob in a
    /// later process makes the Responses API reject the whole request as an invalid payload. Stripping it
    /// keeps the persisted history safe to replay, at the cost of losing the model's saved reasoning trace.
    /// </remarks>
    private IEnumerable<ChatMessage> FilterStripReasoningContent(IEnumerable<ChatMessage> messages)
    {
        foreach (var message in messages)
        {
            if (!message.Contents.Any(c => c is TextReasoningContent))
            {
                yield return message;
                continue;
            }

            var remainingContents = message.Contents.Where(c => c is not TextReasoningContent).ToList();
            if (remainingContents.Count == 0)
            {
                // The message was pure reasoning with nothing else to replay; drop it entirely.
                continue;
            }

            yield return new ChatMessage(message.Role, remainingContents)
            {
                AuthorName = message.AuthorName,
                MessageId = message.MessageId,
                CreatedAt = message.CreatedAt,
            };
        }
    }

    private List<ChatMessage> LoadFromDisk(string filePath)
    {
        if (!File.Exists(filePath))
        {
            return [];
        }

        try
        {
            using var stream = File.OpenRead(filePath);
            return JsonSerializer.Deserialize<List<ChatMessage>>(stream, AIJsonUtilities.DefaultOptions) ?? [];
        }
        catch (JsonException ex)
        {
            this._logger.LogWarning(
                ex,
                "Unreadable history in '{FilePath}'. Likely a format change after a package update; resetting with a backup.",
                filePath);
            BackupFile(filePath, "désérialisation impossible au chargement");
            return [];
        }
    }

    private void BackupFile(string filePath, string reason)
    {
        if (!File.Exists(filePath))
        {
            return;
        }

        var backupPath = $"{filePath}.bak-{DateTime.Now:yyyyMMdd-HHmmss}.json";
        File.Copy(filePath, backupPath, overwrite: true);
        this._logger.LogInformation("History backed up to '{BackupPath}' ({Reason}).", backupPath, reason);
    }

    private async Task SaveToDiskAsync(CancellationToken cancellationToken)
    {
        await using var stream = File.Create(this._filePath);
        await JsonSerializer.SerializeAsync(stream, this._messages, AIJsonUtilities.DefaultOptions, cancellationToken).ConfigureAwait(false);
    }
}

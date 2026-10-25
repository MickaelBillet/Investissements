using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;

namespace AgentAI;

/// <summary>
/// Logs one line per model call (message count, duration, input/output/reasoning tokens): a stalled agent is
/// otherwise indistinguishable from a slow one.
/// </summary>
/// <remarks>
/// Only <see cref="GetResponseAsync"/> is logged: the hosts never stream, since streaming with server-side storage
/// disabled answered <c>400 invalid_payload</c>.
/// </remarks>
internal sealed class ModelCallLoggingChatClient(IChatClient innerClient, AgentKind kind, ILogger logger) : DelegatingChatClient(innerClient)
{
    public override async Task<ChatResponse> GetResponseAsync(IEnumerable<ChatMessage> messages, ChatOptions? options = null, CancellationToken cancellationToken = default)
    {
        // Materialized once: the count is logged and the inner client must not enumerate a lazy sequence twice.
        var messageList = messages.ToList();
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        logger.LogInformation("Agent {Kind}: model call with {Count} messages", kind, messageList.Count);
        try
        {
            var response = await base.GetResponseAsync(messageList, options, cancellationToken).ConfigureAwait(false);
            logger.LogInformation(
                "Agent {Kind}: model answered in {Seconds:0.0}s (in={In}, out={Out}, reasoning={Reasoning})",
                kind, stopwatch.Elapsed.TotalSeconds, response.Usage?.InputTokenCount, response.Usage?.OutputTokenCount, response.Usage?.ReasoningTokenCount);
            return response;
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Agent {Kind}: model call failed after {Seconds:0.0}s", kind, stopwatch.Elapsed.TotalSeconds);
            throw;
        }
    }
}

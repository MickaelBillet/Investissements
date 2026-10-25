using AgentAI;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace InvestissementsDashboard.Maui.Tests;

public class ModelCallLoggingChatClientTests
{
    private readonly Mock<IChatClient> _inner = new();
    private readonly ListLogger _logger = new();

    [Fact]
    public async Task GetResponseAsync_Success_ForwardsMessagesAndLogsCallAndUsage()
    {
        var response = new ChatResponse(new ChatMessage(ChatRole.Assistant, "ok"))
        {
            Usage = new UsageDetails { InputTokenCount = 120, OutputTokenCount = 30, ReasoningTokenCount = 7 },
        };
        IEnumerable<ChatMessage>? forwarded = null;
        _inner
            .Setup(c => c.GetResponseAsync(It.IsAny<IEnumerable<ChatMessage>>(), It.IsAny<ChatOptions?>(), It.IsAny<CancellationToken>()))
            .Callback<IEnumerable<ChatMessage>, ChatOptions?, CancellationToken>((m, _, _) => forwarded = m)
            .ReturnsAsync(response);
        var client = new ModelCallLoggingChatClient(_inner.Object, AgentKind.Allocation, _logger);

        var result = await client.GetResponseAsync([new ChatMessage(ChatRole.User, "a"), new ChatMessage(ChatRole.User, "b")]);

        Assert.Same(response, result);
        Assert.Equal(2, forwarded!.Count());
        Assert.Collection(
            _logger.Entries,
            e => Assert.Equal((LogLevel.Information, "Agent Allocation: model call with 2 messages"), (e.Level, e.Message)),
            e =>
            {
                Assert.Equal(LogLevel.Information, e.Level);
                Assert.Contains("in=120, out=30, reasoning=7", e.Message);
            });
    }

    [Fact]
    public async Task GetResponseAsync_InnerFails_LogsWarningAndRethrows()
    {
        var failure = new HttpRequestException("timeout");
        _inner
            .Setup(c => c.GetResponseAsync(It.IsAny<IEnumerable<ChatMessage>>(), It.IsAny<ChatOptions?>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(failure);
        var client = new ModelCallLoggingChatClient(_inner.Object, AgentKind.Context, _logger);

        var thrown = await Assert.ThrowsAsync<HttpRequestException>(() => client.GetResponseAsync([new ChatMessage(ChatRole.User, "a")]));

        Assert.Same(failure, thrown);
        var warning = Assert.Single(_logger.Entries, e => e.Level == LogLevel.Warning);
        Assert.Same(failure, warning.Exception);
        Assert.StartsWith("Agent Context: model call failed after", warning.Message);
    }

    private sealed class ListLogger : ILogger
    {
        public List<(LogLevel Level, string Message, Exception? Exception)> Entries { get; } = [];

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
            => Entries.Add((logLevel, formatter(state, exception), exception));
    }
}

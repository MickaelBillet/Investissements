using AgentAI;
using InvestissementsDashboard.Shared.Models;
using Microsoft.Agents.AI;
using Microsoft.Agents.AI.Workflows;
using Microsoft.Extensions.AI;
using Moq;
using Moq.Protected;
using Xunit;

namespace InvestissementsDashboard.Maui.Tests;

public class AllocationExecutorTests
{
    private const string AllocationJson = """
        {
          "date": "2026-10-19",
          "totalValue": 1500.0,
          "lines": [
            { "name": "MSCI World", "isin": "IE00B4L5Y983", "assetClass": "Stocks", "sector": "Diversified", "region": "World", "value": 1000.0, "weight": 66.67, "unrealizedGain": 200.0, "roi": 25.0 },
            { "name": "Livret A", "isin": null, "assetClass": "Cash", "sector": "Not Defined", "region": "France", "value": 500.0, "weight": 33.33, "unrealizedGain": null, "roi": null }
          ]
        }
        """;

    private readonly Mock<AIAgent> _agent = new();
    private readonly Mock<IWorkflowContext> _context = new();
    private IEnumerable<ChatMessage>? _sentMessages;

    public AllocationExecutorTests()
    {
        _agent
            .Protected()
            .Setup<Task<AgentResponse>>(
                "RunCoreAsync",
                ItExpr.IsAny<IEnumerable<ChatMessage>>(),
                ItExpr.IsAny<AgentSession?>(),
                ItExpr.IsAny<AgentRunOptions?>(),
                ItExpr.IsAny<CancellationToken>())
            .Callback<IEnumerable<ChatMessage>, AgentSession?, AgentRunOptions?, CancellationToken>((messages, _, _, _) => _sentMessages = messages.ToList())
            .ReturnsAsync(new AgentResponse(new ChatMessage(ChatRole.Assistant, AllocationJson)));
    }

    private ValueTask<AllocationResult> HandleAsync() =>
        new AllocationExecutor(_agent.Object).HandleAsync(new AnalysisRequest(new DateOnly(2026, 10, 19)), _context.Object);

    [Fact]
    public async Task HandleAsync_AgentReturnsJson_ReturnsDeserializedAllocation()
    {
        var result = await HandleAsync();

        Assert.Equal(new DateOnly(2026, 10, 19), result.Date);
        Assert.Equal(1500m, result.TotalValue);
        Assert.Equal(2, result.Lines.Count);
        Assert.Equal("MSCI World", result.Lines[0].Name);
        Assert.Equal("World", result.Lines[0].Region);
        Assert.Null(result.Lines[1].Isin);
    }

    [Fact]
    public async Task HandleAsync_GainAndRoi_KeepsValuesAndNulls()
    {
        var result = await HandleAsync();

        Assert.Equal(200m, result.Lines[0].UnrealizedGain);
        Assert.Equal(25m, result.Lines[0].Roi);
        Assert.Null(result.Lines[1].UnrealizedGain);
        Assert.Null(result.Lines[1].Roi);
    }

    [Fact]
    public async Task HandleAsync_AgentReturnsJson_EmitsAllocationRetrievedEvent()
    {
        await HandleAsync();

        _context.Verify(c => c.AddEventAsync(It.IsAny<AllocationRetrievedEvent>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task HandleAsync_Request_SendsRequestedDateToAgent()
    {
        await HandleAsync();

        Assert.NotNull(_sentMessages);
        Assert.Contains(_sentMessages, m => m.Text.Contains("2026-10-19"));
    }
}

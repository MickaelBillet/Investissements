using AgentAI;
using Microsoft.Agents.AI;
using Microsoft.Agents.AI.Workflows;
using Microsoft.Extensions.AI;
using Moq;
using Moq.Protected;
using Xunit;

namespace InvestissementsDashboard.Maui.Tests;

public class ContextExecutorTests
{
    // The model's date differs from the request on purpose: the executor must keep the requested one.
    private const string ContextJson = """
        {
          "date": "2026-10-18",
          "situation": "La BCE maintient son taux de dépôt à 2 %.",
          "factors": [
            {
              "description": "Hausse des droits de douane américains sur l'acier européen à 50 %",
              "type": "Risk",
              "assetClasses": ["Stocks"],
              "sectors": ["Materials", "Automotive"],
              "regions": ["Europe"],
              "horizon": "ShortTerm",
              "sources": [ { "url": "https://news.test/acier", "publishedOn": "2026-10-15" } ]
            }
          ],
          "indicators": [
            { "code": "ECB_DEPOSIT_RATE", "value": 2.0, "asOf": "2026-10-16", "valueThreeMonthsAgo": 2.0, "asOfThreeMonthsAgo": "2026-07-17", "change3M": 0, "changeUnit": "BasisPoints", "source": "https://ecb.test" },
            { "code": "BRENT_USD", "value": null, "asOf": null, "valueThreeMonthsAgo": null, "asOfThreeMonthsAgo": null, "change3M": null, "changeUnit": "Percent", "source": null }
          ]
        }
        """;

    private readonly Mock<AIAgent> _agent = new();
    private readonly Mock<IWorkflowContext> _context = new();
    private IEnumerable<ChatMessage>? _sentMessages;

    public ContextExecutorTests()
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
            .ReturnsAsync(new AgentResponse(new ChatMessage(ChatRole.Assistant, ContextJson)));
    }

    private ValueTask<PortfolioContext> HandleAsync() =>
        new ContextExecutor(_agent.Object).HandleAsync(new AnalysisRequest(new DateOnly(2026, 10, 19)), _context.Object);

    [Fact]
    public async Task HandleAsync_AgentReturnsJson_KeepsRequestedDate()
    {
        var result = await HandleAsync();

        Assert.Equal(new DateOnly(2026, 10, 19), result.Date);
    }

    [Fact]
    public async Task HandleAsync_AgentReturnsJson_DeserializesFactorsWithEnumNames()
    {
        var result = await HandleAsync();

        Assert.Equal("La BCE maintient son taux de dépôt à 2 %.", result.Situation);
        var factor = Assert.Single(result.Factors);
        Assert.Equal(FactorType.Risk, factor.Type);
        Assert.Equal(FactorHorizon.ShortTerm, factor.Horizon);
        Assert.Equal(["Materials", "Automotive"], factor.Sectors);
        Assert.Equal(new ContextSource("https://news.test/acier", "2026-10-15"), Assert.Single(factor.Sources));
    }

    [Fact]
    public async Task HandleAsync_IndicatorNotFound_KeepsNullsAndUnit()
    {
        var result = await HandleAsync();

        Assert.Equal(2, result.Indicators.Count);
        Assert.Equal(2.0m, result.Indicators[0].Value);
        Assert.Equal(ChangeUnit.BasisPoints, result.Indicators[0].ChangeUnit);
        var brent = result.Indicators[1];
        Assert.Null(brent.Value);
        Assert.Null(brent.Change3M);
        Assert.Equal(ChangeUnit.Percent, brent.ChangeUnit);
    }

    [Fact]
    public async Task HandleAsync_AgentReturnsJson_EmitsContextRetrievedEvent()
    {
        await HandleAsync();

        _context.Verify(c => c.AddEventAsync(It.IsAny<ContextRetrievedEvent>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task HandleAsync_Request_SendsRequestedDateToAgent()
    {
        await HandleAsync();

        Assert.NotNull(_sentMessages);
        Assert.Contains(_sentMessages, m => m.Text.Contains("2026-10-19"));
    }

    [Fact]
    public void ContextRetrievedEvent_ToString_CountsFactorsAndFilledIndicators()
    {
        var context = new PortfolioContext(
            new DateOnly(2026, 10, 19),
            string.Empty,
            [],
            [
                new ContextIndicator("EURUSD", 1.16m, "2026-10-17", 1.17m, "2026-07-17", -0.9m, ChangeUnit.Percent, null),
                new ContextIndicator("GOLD_USD", null, null, null, null, null, ChangeUnit.Percent, null),
            ]);

        Assert.Equal("Contexte récupéré : 0 facteurs, 1/2 indicateurs renseignés", new ContextRetrievedEvent(context).ToString());
    }
}

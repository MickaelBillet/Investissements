using AgentAI;
using Microsoft.Agents.AI.Workflows;
using Moq;
using Xunit;

namespace InvestissementsDashboard.Maui.Tests;

public class ContextExecutorTests
{
    [Fact]
    public async Task HandleAsync_Request_ReturnsContextForTheRequestedDate()
    {
        var result = await new ContextExecutor().HandleAsync(new AnalysisRequest(new DateOnly(2026, 10, 19)), new Mock<IWorkflowContext>().Object);

        Assert.Equal(new DateOnly(2026, 10, 19), result.Date);
    }
}

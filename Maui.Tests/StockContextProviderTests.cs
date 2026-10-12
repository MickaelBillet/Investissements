#pragma warning disable MAAI001 // InvokingContext is flagged evaluation-only by the Agent Framework.

using AgentAI;
using Microsoft.Agents.AI;
using Moq;
using Xunit;

namespace InvestissementsDashboard.Maui.Tests;

public class StockContextProviderTests
{
    private static async Task<string?> GetInstructionsAsync(string stockName, string? isin)
    {
        var provider = new StockContextProvider(stockName, isin);
        var context = new AIContextProvider.InvokingContext(new Mock<AIAgent>().Object, null, new AIContext());

        var result = await provider.InvokingAsync(context);

        return result.Instructions;
    }

    [Fact]
    public async Task ProvideAIContext_WithIsin_IncludesIsinInInstructions()
    {
        var instructions = await GetInstructionsAsync("Air Liquide", " FR0000120073 ");

        Assert.Contains("Air Liquide (ISIN : FR0000120073)", instructions);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task ProvideAIContext_WithoutIsin_KeepsNameOnlyInstructions(string? isin)
    {
        var instructions = await GetInstructionsAsync("Air Liquide", isin);

        Assert.Contains("Action à analyser : Air Liquide.", instructions);
        Assert.DoesNotContain("ISIN", instructions);
    }
}

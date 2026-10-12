#pragma warning disable MAAI001 // InvokingContext is flagged evaluation-only by the Agent Framework.

using AgentAI;
using Microsoft.Agents.AI;
using Moq;
using Xunit;

namespace InvestissementsDashboard.Maui.Tests;

public class NewsContextProviderTests
{
    private static async Task<string?> GetInstructionsAsync(string company, string? isin)
    {
        var provider = new NewsContextProvider(company, isin, days: 7, new FixedTimeProvider(new DateTimeOffset(2026, 10, 9, 12, 0, 0, TimeSpan.Zero)));
        var context = new AIContextProvider.InvokingContext(new Mock<AIAgent>().Object, null, new AIContext());

        var result = await provider.InvokingAsync(context);

        return result.Instructions;
    }

    [Fact]
    public async Task ProvideAIContext_WithIsin_IncludesTrimmedIsin()
    {
        var instructions = await GetInstructionsAsync("Air Liquide", " FR0000120073 ");

        Assert.Contains("Société à suivre : Air Liquide.", instructions);
        Assert.Contains("ISIN : FR0000120073.", instructions);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task ProvideAIContext_WithoutIsin_StatesItIsNotProvided(string? isin)
    {
        var instructions = await GetInstructionsAsync("Air Liquide", isin);

        Assert.Contains("ISIN : non fourni.", instructions);
    }

    private sealed class FixedTimeProvider(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;
    }
}

using AgentAI.Core.Agents;
using Xunit;

namespace InvestissementsDashboard.Maui.Tests;

public class AgentInstructionsTests
{
    [Theory]
    [InlineData("ActualitesSociete.md")]
    [InlineData("AnalyseAction.md")]
    [InlineData("AllocationPortefeuille.md")]
    [InlineData("Context.md")]
    [InlineData("Synthesis.md")]
    public void Instructions_SingleTurnRule_IsPresent(string fileName)
    {
        // InstructionLoader is internal, so the embedded resource is read directly.
        using var stream = typeof(AgentAIOptions).Assembly.GetManifestResourceStream($"Instructions.{fileName}");
        Assert.NotNull(stream);
        using var reader = new StreamReader(stream);

        Assert.Contains("Échange à tour unique", reader.ReadToEnd());
    }

    [Fact]
    public void Instructions_Synthesis_DoNotAnnounceCeilings()
    {
        using var stream = typeof(AgentAIOptions).Assembly.GetManifestResourceStream("Instructions.Synthesis.md");
        Assert.NotNull(stream);
        using var reader = new StreamReader(stream);

        Assert.DoesNotContain("plafond", reader.ReadToEnd(), StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Instructions_Allocation_RequiresTheWatchlistTool()
    {
        using var stream = typeof(AgentAIOptions).Assembly.GetManifestResourceStream("Instructions.AllocationPortefeuille.md");
        Assert.NotNull(stream);
        using var reader = new StreamReader(stream);

        Assert.Contains("`get_watchlist`", reader.ReadToEnd());
    }
}

using AgentAI.Core.Agents;
using Xunit;

namespace InvestissementsDashboard.Maui.Tests;

public class AgentInstructionsTests
{
    [Theory]
    [InlineData("News.md")]
    [InlineData("Stock.md")]
    [InlineData("Allocation.md")]
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

    // The only place stating this rule: the executor's message no longer repeats it.
    [Fact]
    public void Instructions_Synthesis_RestrictInitiateToTheWatchlist()
    {
        using var stream = typeof(AgentAIOptions).Assembly.GetManifestResourceStream("Instructions.Synthesis.md");
        Assert.NotNull(stream);
        using var reader = new StreamReader(stream);

        Assert.Contains("seule source autorisée pour une recommandation de type « Initier »", reader.ReadToEnd());
    }

    [Fact]
    public void Instructions_Allocation_RequiresTheWatchlistTool()
    {
        using var stream = typeof(AgentAIOptions).Assembly.GetManifestResourceStream("Instructions.Allocation.md");
        Assert.NotNull(stream);
        using var reader = new StreamReader(stream);

        Assert.Contains("`get_watchlist`", reader.ReadToEnd());
    }

    // The scenarios are watched through these signals, while the Context agent must not know the scenarios themselves.
    [Theory]
    [InlineData("Ormuz")]
    [InlineData("Treasury 30 ans")]
    [InlineData("PPI")]
    [InlineData("productivité horaire")]
    [InlineData("tokens")]
    public void Instructions_Context_CoverTheScenarioSignals(string signal)
    {
        using var stream = typeof(AgentAIOptions).Assembly.GetManifestResourceStream("Instructions.Context.md");
        Assert.NotNull(stream);
        using var reader = new StreamReader(stream);

        Assert.Contains(signal, reader.ReadToEnd());
    }

    // The repository is public, so the profile describes the drivers without naming positions: the model does the mapping.
    [Fact]
    public void Instructions_InvestorProfile_AsksToMapLinesToDrivers()
    {
        using var stream = typeof(AgentAIOptions).Assembly.GetManifestResourceStream("Instructions.InvestorProfile.md");
        Assert.NotNull(stream);
        using var reader = new StreamReader(stream);

        Assert.Contains("Rattache chaque ligne de l'allocation au driver", reader.ReadToEnd());
    }

    // Synthesis.md only accepts a scenario or a context factor as a driver: the third-party guide must not become one.
    [Fact]
    public void Instructions_InvestorProfile_GuidingThreadIsNotADriver()
    {
        using var stream = typeof(AgentAIOptions).Assembly.GetManifestResourceStream("Instructions.InvestorProfile.md");
        Assert.NotNull(stream);
        using var reader = new StreamReader(stream);

        Assert.Contains("Il ne suffit pas, à lui seul, comme moteur d'une recommandation", reader.ReadToEnd());
    }

    [Fact]
    public void Instructions_InvestorProfile_ScenariosPrevailOverGuidingThread()
    {
        using var stream = typeof(AgentAIOptions).Assembly.GetManifestResourceStream("Instructions.InvestorProfile.md");
        Assert.NotNull(stream);
        using var reader = new StreamReader(stream);

        Assert.Contains("Les scénarios de l'investisseur prévalent sur ce fil directeur", reader.ReadToEnd());
    }
}

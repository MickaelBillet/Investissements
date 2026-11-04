using AgentAI.Core.Agents;
using Xunit;

namespace InvestissementsDashboard.Maui.Tests;

public class AgentInstructionsTests
{
    [Theory]
    [InlineData("News.md")]
    [InlineData("Stock.md")]
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

    // Rules the first real reports broke: each one is checked so that it cannot silently disappear.
    [Theory]
    [InlineData("Une conviction, le fil directeur ou la présence dans la watchlist ne sont pas des moteurs")]
    [InlineData("le nombre ou la petite taille des lignes de crowdlending et de dette privée")]
    [InlineData("une part des seules lignes éligibles de la classe d'actifs")]
    [InlineData("une entrée pour chaque scénario du profil")]
    [InlineData("Une ligne de valeur nulle avec un `roi` négatif est une perte constatée")]
    public void Instructions_Synthesis_StateTheReportRules(string rule)
    {
        using var stream = typeof(AgentAIOptions).Assembly.GetManifestResourceStream("Instructions.Synthesis.md");
        Assert.NotNull(stream);
        using var reader = new StreamReader(stream);

        Assert.Contains(rule, reader.ReadToEnd());
    }

    [Fact]
    public void Instructions_InvestorProfile_NamesTheCurrentRegime()
    {
        using var stream = typeof(AgentAIOptions).Assembly.GetManifestResourceStream("Instructions.InvestorProfile.md");
        Assert.NotNull(stream);
        using var reader = new StreamReader(stream);

        Assert.Contains("Le régime en cours est S3", reader.ReadToEnd());
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

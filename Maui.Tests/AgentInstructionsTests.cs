using AgentAI;
using Xunit;

namespace InvestissementsDashboard.Maui.Tests;

public class AgentInstructionsTests
{
    [Theory]
    [InlineData("ActualitesSociete.md")]
    [InlineData("AnalyseAction.md")]
    public void Instructions_SingleTurnRule_IsPresent(string fileName)
    {
        // InstructionLoader is internal, so the embedded resource is read directly.
        using var stream = typeof(AgentAIOptions).Assembly.GetManifestResourceStream($"Instructions.{fileName}");
        Assert.NotNull(stream);
        using var reader = new StreamReader(stream);

        Assert.Contains("Échange à tour unique", reader.ReadToEnd());
    }
}

using System.Reflection;

namespace AgentAI;

/// <summary>
/// Loads agent prompts embedded in this assembly (see <c>Instructions/*.md</c>).
/// </summary>
/// <remarks>
/// Prompts are embedded rather than copied next to the executable so the library does not depend on
/// the host's base directory layout, which differs between a console app and a packaged MAUI app.
/// </remarks>
internal static class InstructionLoader
{
    public static string Load(string fileName)
    {
        var resourceName = $"Instructions.{fileName}";
        using var stream = typeof(InstructionLoader).Assembly.GetManifestResourceStream(resourceName)
            ?? throw new InvalidOperationException($"Embedded instruction '{resourceName}' not found.");
        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }
}

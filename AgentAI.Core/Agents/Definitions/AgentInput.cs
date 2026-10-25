namespace AgentAI;

internal static class AgentInput
{
    /// <summary>Returns <paramref name="contextInput"/>, or fails with a message the host can show as is.</summary>
    /// <param name="expected">What the agent needs, in French, e.g. "une ville".</param>
    public static string Require(string? contextInput, string expected)
    {
        return string.IsNullOrWhiteSpace(contextInput)
            ? throw new ArgumentException($"Cet agent nécessite {expected}.", nameof(contextInput))
            : contextInput;
    }
}

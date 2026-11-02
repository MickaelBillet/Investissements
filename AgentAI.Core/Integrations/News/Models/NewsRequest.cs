namespace AgentAI;

/// <summary>
/// What a <see cref="INewsFeedSource"/> is asked to look up.
/// </summary>
/// <param name="Company">Company name.</param>
/// <param name="Isin">Optional ISIN, for sources keyed on a security rather than a name.</param>
/// <param name="Days">Size of the look-back window, counted back from today.</param>
public sealed record NewsRequest(string Company, string? Isin, int Days);

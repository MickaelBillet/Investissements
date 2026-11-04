namespace AgentAI.Core.Workflows.PortfolioReview.Messages;

/// <param name="PublishedOn">As written by the source; text rather than a date so that a partial date cannot fail the whole step.</param>
public record ContextSource(string Url, string? PublishedOn);

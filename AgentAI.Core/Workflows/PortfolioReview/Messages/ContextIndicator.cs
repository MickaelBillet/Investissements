namespace AgentAI.Core.Workflows.PortfolioReview.Messages;

/// <summary>
/// A key indicator (<c>ECB_DEPOSIT_RATE</c>, <c>EURUSD</c>…) now and three months ago. Values not found are
/// <see langword="null"/>, never 0, so the synthesis cannot mistake a missing figure for a real one.
/// </summary>
/// <param name="AsOf">Date of <paramref name="Value"/>; text because monthly figures are dated by month only.</param>
/// <param name="Change3M">In basis points for rates, in percent otherwise (<paramref name="ChangeUnit"/>).</param>
public record ContextIndicator(
    string Code,
    decimal? Value,
    string? AsOf,
    decimal? ValueThreeMonthsAgo,
    string? AsOfThreeMonthsAgo,
    decimal? Change3M,
    ChangeUnit ChangeUnit,
    string? Source);

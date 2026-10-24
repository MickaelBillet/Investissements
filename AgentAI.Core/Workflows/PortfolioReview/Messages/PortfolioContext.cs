using System.Text.Json.Serialization;

namespace AgentAI;

/// <summary>
/// Economic, financial and geopolitical context at the analysis date, as established by the Context agent
/// (structured output of <c>Context.md</c>).
/// </summary>
/// <param name="Situation">Summary of the situation, organised by theme.</param>
public record PortfolioContext(
    DateOnly Date,
    string Situation,
    IReadOnlyList<ContextFactor> Factors,
    IReadOnlyList<ContextIndicator> Indicators);

/// <summary>A concrete element likely to move the markets in the next 6 to 12 months.</summary>
/// <param name="AssetClasses">Labels of the application's reference list (<c>Stocks</c>, <c>Bonds</c>…), so the synthesis can match them with the portfolio.</param>
public record ContextFactor(
    string Description,
    FactorType Type,
    IReadOnlyList<string> AssetClasses,
    IReadOnlyList<string> Sectors,
    IReadOnlyList<string> Regions,
    FactorHorizon Horizon,
    IReadOnlyList<ContextSource> Sources);

/// <param name="PublishedOn">As written by the source; text rather than a date so that a partial date cannot fail the whole step.</param>
public record ContextSource(string Url, string? PublishedOn);

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

// The enums are written as names in the JSON schema sent to the model, which reads and writes them as text.

[JsonConverter(typeof(JsonStringEnumConverter<FactorType>))]
public enum FactorType
{
    Risk,
    Support,
}

[JsonConverter(typeof(JsonStringEnumConverter<FactorHorizon>))]
public enum FactorHorizon
{
    /// <summary>Less than 6 months.</summary>
    ShortTerm,

    /// <summary>6 to 18 months.</summary>
    MediumTerm,
}

[JsonConverter(typeof(JsonStringEnumConverter<ChangeUnit>))]
public enum ChangeUnit
{
    BasisPoints,
    Percent,
}

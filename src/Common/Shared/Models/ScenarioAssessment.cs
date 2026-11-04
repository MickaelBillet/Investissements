namespace InvestissementsDashboard.Shared.Models;

/// <summary>
/// What the context of the day says about one scenario of the investor (structured output of <c>Synthesis.md</c>).
/// </summary>
/// <param name="Scenario">Identifier and name of the scenario, as written in the investor's profile.</param>
/// <param name="Facts">The facts of the context behind the verdict.</param>
/// <param name="PortfolioImpact">What the scenario means for the portfolio, according to the investor's drivers.</param>
public record ScenarioAssessment(
    string Scenario,
    ScenarioVerdict Verdict,
    string Facts,
    string PortfolioImpact);

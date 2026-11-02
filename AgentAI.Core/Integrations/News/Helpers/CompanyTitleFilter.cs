using System.Globalization;
using System.Text;

namespace AgentAI;

/// <summary>
/// Keeps only the articles whose title names the company. Pure: no I/O.
/// </summary>
/// <remarks>
/// Needed for feeds that match a phrase anywhere in the article body: some results never name the
/// company in their title, and the agent only sees titles, so it would have to flag them as ambiguous.
/// </remarks>
internal static class CompanyTitleFilter
{
    // Dropped from the end of a company name before matching titles: headlines rarely carry the legal form.
    private static readonly string[] LegalSuffixes = ["nv", "sa", "se", "ag", "plc", "inc", "ltd", "corp", "corporation", "spa", "srl", "gmbh", "oyj", "ab"];

    public static IReadOnlyList<NewsArticle> Filter(IReadOnlyList<NewsArticle> articles, string company)
    {
        var words = NormalizeForMatch(company).Split(' ', StringSplitOptions.RemoveEmptyEntries).ToList();
        while (words.Count > 1 && LegalSuffixes.Contains(words[^1]))
        {
            words.RemoveAt(words.Count - 1);
        }

        if (words.Count == 0)
        {
            // Nothing usable to match on: keeping everything beats silently dropping every article.
            return articles;
        }

        // Padding with spaces makes the match whole-word, so "ARM" does not match "farm".
        var needle = $" {string.Join(' ', words)} ";
        return articles.Where(article => $" {NormalizeForMatch(article.Title)} ".Contains(needle, StringComparison.Ordinal)).ToList();
    }

    /// <summary>
    /// Lowercases, strips accents and turns every non-alphanumeric character into a single space.
    /// </summary>
    private static string NormalizeForMatch(string text)
    {
        var builder = new StringBuilder(text.Length);
        foreach (var c in text.Normalize(NormalizationForm.FormD))
        {
            if (CharUnicodeInfo.GetUnicodeCategory(c) == UnicodeCategory.NonSpacingMark)
            {
                continue;
            }

            builder.Append(char.IsLetterOrDigit(c) ? char.ToLowerInvariant(c) : ' ');
        }

        return string.Join(' ', builder.ToString().Split(' ', StringSplitOptions.RemoveEmptyEntries));
    }
}

using InvestissementsDashboard.Client.Resources;
using Microsoft.Extensions.Localization;

namespace InvestissementsDashboard.Client.Services;

public sealed class LocalizationService(IStringLocalizer<Translations> localizer) : ILocalizationService
{
    public string Translate(string key)
    {
        var normalized = Normalize(key);
        var localized  = localizer[normalized];
        return localized.ResourceNotFound ? key : localized.Value;
    }

    // Normalise les clés avec caractères spéciaux (ex: "Direct loans (P2P)" → "Direct_loans_P2P")
    private static string Normalize(string key) =>
        key.Replace(" ", "_").Replace("(", "").Replace(")", "");
}

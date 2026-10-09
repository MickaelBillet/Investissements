using InvestissementsDashboard.Client.Services;

namespace InvestissementsDashboard.Maui.Services;

/// <summary>
/// Stores the agent settings in MAUI <see cref="IPreferences"/>. Plain storage is enough: these values are not secrets
/// (the Foundry endpoint is protected by Entra ID), unlike the keys kept in <see cref="IKeyValueStore"/>.
/// </summary>
public sealed class PreferencesAgentSettings : IAgentSettings
{
    internal const string EndpointKey = "agent.foundry.endpoint";
    internal const string ModelKey = "agent.foundry.model";
    internal const string InvestZaptoBaseUrlKey = "agent.investzapto.baseurl";

    private readonly IPreferences preferences;

    /// <param name="preferences">Backing store.</param>
    /// <param name="defaultInvestZaptoBaseUrl">Stored on first launch only, so a later edit of the preference is never overwritten.</param>
    public PreferencesAgentSettings(IPreferences preferences, string defaultInvestZaptoBaseUrl)
    {
        this.preferences = preferences;
        if (Read(InvestZaptoBaseUrlKey) is null)
        {
            preferences.Set(InvestZaptoBaseUrlKey, defaultInvestZaptoBaseUrl);
        }
    }

    public string? InvestZaptoBaseUrl => Read(InvestZaptoBaseUrlKey);

    public string? FoundryEndpoint => Read(EndpointKey);

    public string? Model => Read(ModelKey);

    public void Save(string? foundryEndpoint, string? model)
    {
        Write(EndpointKey, foundryEndpoint);
        Write(ModelKey, model);
    }

    private string? Read(string key)
    {
        var value = preferences.Get<string?>(key, null);
        return string.IsNullOrWhiteSpace(value) ? null : value;
    }

    private void Write(string key, string? value)
    {
        // Removing rather than storing an empty string keeps "not configured" a single state.
        if (string.IsNullOrWhiteSpace(value))
        {
            preferences.Remove(key);
        }
        else
        {
            preferences.Set(key, value);
        }
    }
}

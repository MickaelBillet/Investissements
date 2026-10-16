using InvestissementsDashboard.Client.Services;

namespace InvestissementsDashboard.Client.ViewModels;

/// <summary>
/// Presentation logic of the settings page. The settings store is optional (only the MAUI host registers it),
/// so <see cref="IsAvailable"/> tells the view whether there is anything to edit.
/// </summary>
public class SettingsViewModel(ILocalizationService localizer, IAgentSettings? settings = null, IKeyValueStore? secrets = null)
{
    public bool IsAvailable => settings is not null;

    public string Endpoint { get; set; } = string.Empty;
    public string Model { get; set; } = string.Empty;

    /// <summary>
    /// New MCP key typed by the user. Never loaded back from the store (it is a secret): left blank, the stored key is kept.
    /// </summary>
    public string McpApiKey { get; set; } = string.Empty;

    /// <summary>Localized outcome of the last save, or <see langword="null"/> before the first one.</summary>
    public string? StatusMessage { get; private set; }

    public bool IsStatusError { get; private set; }

    public void Load()
    {
        Endpoint = settings?.FoundryEndpoint ?? string.Empty;
        Model = settings?.Model ?? string.Empty;
        McpApiKey = string.Empty;
        StatusMessage = null;
        IsStatusError = false;
    }

    public async Task SaveAsync()
    {
        if (settings is null)
        {
            throw new InvalidOperationException("No IAgentSettings is registered for this host.");
        }

        var endpoint = Endpoint.Trim();
        // https only: the agent calls carry an Entra token, which must never travel over plain http.
        if (!Uri.TryCreate(endpoint, UriKind.Absolute, out var uri) || uri.Scheme != Uri.UriSchemeHttps)
        {
            IsStatusError = true;
            StatusMessage = localizer.Translate("Settings_EndpointInvalid");
            return;
        }

        var model = Model.Trim();
        settings.Save(endpoint, model.Length == 0 ? null : model);

        var mcpApiKey = McpApiKey.Trim();
        if (mcpApiKey.Length > 0 && secrets is not null)
        {
            await secrets.SetAsync(AgentSecretKeys.McpApiKey, mcpApiKey);
            McpApiKey = string.Empty;
        }

        IsStatusError = false;
        StatusMessage = localizer.Translate("Settings_Saved");
    }
}

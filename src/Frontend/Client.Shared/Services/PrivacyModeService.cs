namespace InvestissementsDashboard.Client.Services;

public class PrivacyModeService(IKeyValueStore store) : IPrivacyModeService
{
    private const string StorageKey = "investissements.hideAmounts";

    public bool IsHidden { get; private set; }

    public event Action? OnChange;

    public async Task InitializeAsync()
    {
        var stored = await store.GetAsync(StorageKey);
        IsHidden = stored == "true";
        OnChange?.Invoke();
    }

    public async Task ToggleAsync()
    {
        IsHidden = !IsHidden;
        await store.SetAsync(StorageKey, IsHidden ? "true" : "false");
        OnChange?.Invoke();
    }
}

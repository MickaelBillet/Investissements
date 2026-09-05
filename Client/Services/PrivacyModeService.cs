using Microsoft.JSInterop;

namespace InvestissementsDashboard.Client.Services;

public class PrivacyModeService(IJSRuntime jsRuntime) : IPrivacyModeService
{
    private const string StorageKey = "investissements.hideAmounts";

    public bool IsHidden { get; private set; }

    public event Action? OnChange;

    public async Task InitializeAsync()
    {
        var stored = await jsRuntime.InvokeAsync<string?>("localStorage.getItem", StorageKey);
        IsHidden = stored == "true";
        OnChange?.Invoke();
    }

    public async Task ToggleAsync()
    {
        IsHidden = !IsHidden;
        await jsRuntime.InvokeVoidAsync("localStorage.setItem", StorageKey, IsHidden ? "true" : "false");
        OnChange?.Invoke();
    }
}

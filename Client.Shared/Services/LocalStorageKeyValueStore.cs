using Microsoft.JSInterop;

namespace InvestissementsDashboard.Client.Services;

public class LocalStorageKeyValueStore(IJSRuntime jsRuntime) : IKeyValueStore
{
    public async Task<string?> GetAsync(string key) =>
        await jsRuntime.InvokeAsync<string?>("localStorage.getItem", key);

    public async Task SetAsync(string key, string value) =>
        await jsRuntime.InvokeVoidAsync("localStorage.setItem", key, value);

    public async Task RemoveAsync(string key) =>
        await jsRuntime.InvokeVoidAsync("localStorage.removeItem", key);
}

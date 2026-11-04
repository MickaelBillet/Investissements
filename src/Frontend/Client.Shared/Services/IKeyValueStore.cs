namespace InvestissementsDashboard.Client.Services;

/// <summary>
/// Persistent string key/value storage. Abstracted so that each host picks the right backing
/// store (browser localStorage for WASM, encrypted SecureStorage for MAUI).
/// </summary>
public interface IKeyValueStore
{
    Task<string?> GetAsync(string key);
    Task SetAsync(string key, string value);
    Task RemoveAsync(string key);
}

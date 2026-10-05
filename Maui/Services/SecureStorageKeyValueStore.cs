using InvestissementsDashboard.Client.Services;
using Microsoft.Extensions.Logging;

namespace InvestissementsDashboard.Maui.Services;

/// <summary>
/// Stores values encrypted by the OS: the dashboard password must not sit in clear text in the WebView profile.
/// </summary>
public class SecureStorageKeyValueStore(ISecureStorage secureStorage, ILogger<SecureStorageKeyValueStore> logger) : IKeyValueStore
{
    public async Task<string?> GetAsync(string key)
    {
        try
        {
            return await secureStorage.GetAsync(key);
        }
        catch (Exception ex)
        {
            // An undecryptable entry (e.g. profile moved to another machine) must behave like a missing one,
            // otherwise the user could never reach the login screen again.
            logger.LogWarning(ex, "Secure storage entry '{Key}' could not be read; treating it as missing.", key);
            secureStorage.Remove(key);
            return null;
        }
    }

    public Task SetAsync(string key, string value) => secureStorage.SetAsync(key, value);

    public Task RemoveAsync(string key)
    {
        secureStorage.Remove(key);
        return Task.CompletedTask;
    }
}

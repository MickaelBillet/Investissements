using Microsoft.JSInterop;

namespace InvestissementsDashboard.Client.Services;

public class SessionService(HttpClient httpClient, IJSRuntime jsRuntime) : ISessionService
{
    private const string StorageKey = "investissements.dashboardPassword";
    private const string PasswordHeader = "x-dashboard-password";

    public bool IsAuthenticated { get; private set; }
    public string? Password { get; private set; }

    public event Action? OnChange;

    public async Task InitializeAsync()
    {
        var stored = await jsRuntime.InvokeAsync<string?>("localStorage.getItem", StorageKey);
        if (string.IsNullOrEmpty(stored))
        {
            IsAuthenticated = false;
            return;
        }

        IsAuthenticated = await VerifyAsync(stored);
        Password = IsAuthenticated ? stored : null;
        OnChange?.Invoke();
    }

    public async Task<bool> LoginAsync(string password)
    {
        var isValid = await VerifyAsync(password);
        if (isValid)
        {
            Password = password;
            IsAuthenticated = true;
            await jsRuntime.InvokeVoidAsync("localStorage.setItem", StorageKey, password);
            OnChange?.Invoke();
        }
        return isValid;
    }

    public async Task LogoutAsync()
    {
        Password = null;
        IsAuthenticated = false;
        await jsRuntime.InvokeVoidAsync("localStorage.removeItem", StorageKey);
        OnChange?.Invoke();
    }

    private async Task<bool> VerifyAsync(string password)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, "api/auth/verify");
        request.Headers.Add(PasswordHeader, password);
        using var response = await httpClient.SendAsync(request);
        return response.IsSuccessStatusCode;
    }
}

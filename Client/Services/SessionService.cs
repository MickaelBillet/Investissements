using System.Text.Json;
using Microsoft.JSInterop;

namespace InvestissementsDashboard.Client.Services;

public class SessionService(HttpClient httpClient, IJSRuntime jsRuntime) : ISessionService
{
    private static readonly TimeSpan SessionDuration = TimeSpan.FromHours(1);

    private const string StorageKey = "investissements.dashboardSession";
    private const string PasswordHeader = "x-dashboard-password";

    private DateTimeOffset _expiresAt;

    public bool IsAuthenticated { get; private set; }
    public bool IsSessionExpired => DateTimeOffset.UtcNow >= _expiresAt;
    public string? Password { get; private set; }

    public event Action? OnChange;

    public async Task InitializeAsync()
    {
        var stored = await jsRuntime.InvokeAsync<string?>("localStorage.getItem", StorageKey);
        var session = Deserialize(stored);

        if (session is null || DateTimeOffset.UtcNow >= session.Value.ExpiresAt)
        {
            IsAuthenticated = false;
            return;
        }

        _expiresAt = session.Value.ExpiresAt;
        IsAuthenticated = await VerifyAsync(session.Value.Password);
        Password = IsAuthenticated ? session.Value.Password : null;
        OnChange?.Invoke();
    }

    public async Task<bool> LoginAsync(string password)
    {
        var isValid = await VerifyAsync(password);
        if (isValid)
        {
            Password = password;
            IsAuthenticated = true;
            _expiresAt = DateTimeOffset.UtcNow.Add(SessionDuration);
            await PersistAsync();
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

    public async Task ExtendSessionAsync()
    {
        if (!IsAuthenticated || Password is null) return;
        _expiresAt = DateTimeOffset.UtcNow.Add(SessionDuration);
        await PersistAsync();
    }

    private async Task<bool> VerifyAsync(string password)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, "api/auth/verify");
        request.Headers.Add(PasswordHeader, password);
        using var response = await httpClient.SendAsync(request);
        return response.IsSuccessStatusCode;
    }

    private Task PersistAsync() =>
        jsRuntime.InvokeVoidAsync("localStorage.setItem", StorageKey,
            JsonSerializer.Serialize(new StoredSession(Password!, _expiresAt))).AsTask();

    private static (string Password, DateTimeOffset ExpiresAt)? Deserialize(string? json)
    {
        if (string.IsNullOrEmpty(json)) return null;
        try
        {
            var session = JsonSerializer.Deserialize<StoredSession>(json);
            return session is null ? null : (session.Password, session.ExpiresAt);
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private sealed record StoredSession(string Password, DateTimeOffset ExpiresAt);
}

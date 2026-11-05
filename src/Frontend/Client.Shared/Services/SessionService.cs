using System.Net;
using System.Text.Json;
using InvestissementsDashboard.Client.Model;
using Microsoft.Extensions.Logging;

namespace InvestissementsDashboard.Client.Services;

public class SessionService(HttpClient httpClient, IKeyValueStore store, ILogger<SessionService> logger) : ISessionService
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
        var stored = await store.GetAsync(StorageKey);
        var session = Deserialize(stored);

        if (session is null || DateTimeOffset.UtcNow >= session.Value.ExpiresAt)
        {
            IsAuthenticated = false;
            return;
        }

        _expiresAt = session.Value.ExpiresAt;
        // The stored session is kept whatever the result: after an outage, the next start verifies it again.
        IsAuthenticated = await VerifyAsync(session.Value.Password) == LoginResult.Success;
        Password = IsAuthenticated ? session.Value.Password : null;
        OnChange?.Invoke();
    }

    public async Task<LoginResult> LoginAsync(string password)
    {
        var result = await VerifyAsync(password);
        if (result == LoginResult.Success)
        {
            Password = password;
            IsAuthenticated = true;
            _expiresAt = DateTimeOffset.UtcNow.Add(SessionDuration);
            await PersistAsync();
            OnChange?.Invoke();
        }
        return result;
    }

    public async Task LogoutAsync()
    {
        Password = null;
        IsAuthenticated = false;
        await store.RemoveAsync(StorageKey);
        OnChange?.Invoke();
    }

    public async Task ExtendSessionAsync()
    {
        if (!IsAuthenticated || Password is null) return;
        _expiresAt = DateTimeOffset.UtcNow.Add(SessionDuration);
        await PersistAsync();
    }

    private async Task<LoginResult> VerifyAsync(string password)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, "api/auth/verify");
        request.Headers.Add(PasswordHeader, password);
        try
        {
            using var response = await httpClient.SendAsync(request);
            if (response.IsSuccessStatusCode)
            {
                return LoginResult.Success;
            }

            // Only a 401 is a verdict on the password: during an outage the proxy answers 500 or 503 without asking the Api.
            if (response.StatusCode == HttpStatusCode.Unauthorized)
            {
                return LoginResult.InvalidPassword;
            }

            logger.LogError("The Api answered {StatusCode} while verifying the password", (int)response.StatusCode);
            return LoginResult.ServiceUnavailable;
        }
        // TaskCanceledException: no caller can cancel this request, so it can only be the HttpClient's timeout.
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            logger.LogError(ex, "The Api could not be reached while verifying the password");
            return LoginResult.ServiceUnavailable;
        }
    }

    private Task PersistAsync() =>
        store.SetAsync(StorageKey,
            JsonSerializer.Serialize(new StoredSession(Password!, _expiresAt)));

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

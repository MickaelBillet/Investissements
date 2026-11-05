using InvestissementsDashboard.Client.Model;

namespace InvestissementsDashboard.Client.Services;

public interface ISessionService
{
    bool IsAuthenticated { get; }
    bool IsSessionExpired { get; }
    string? Password { get; }
    event Action? OnChange;
    Task InitializeAsync();
    Task<LoginResult> LoginAsync(string password);
    Task LogoutAsync();
    Task ExtendSessionAsync();
}

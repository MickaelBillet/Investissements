using InvestissementsDashboard.Client.Model;
using InvestissementsDashboard.Client.Services;

namespace InvestissementsDashboard.Client.ViewModels;

public class LoginGateViewModel(ISessionService sessionService)
{
    public string Password { get; set; } = string.Empty;

    /// <summary>The resource key of the message to show, or null when there is none.</summary>
    public string? ErrorKey { get; private set; }

    public bool IsSubmitting { get; private set; }

    public async Task SubmitAsync()
    {
        IsSubmitting = true;
        // Cleared first: the Api can take 45 s to fail, and the previous message would be misleading meanwhile.
        ErrorKey = null;
        ErrorKey = await sessionService.LoginAsync(Password) switch
        {
            LoginResult.InvalidPassword => "Login_InvalidPassword",
            LoginResult.ServiceUnavailable => "Login_ServiceUnavailable",
            _ => null
        };
        IsSubmitting = false;
    }
}

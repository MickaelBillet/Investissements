using InvestissementsDashboard.Client.Services;

namespace InvestissementsDashboard.Client.ViewModels;

public class LoginGateViewModel(ISessionService sessionService)
{
    public string Password { get; set; } = string.Empty;
    public bool ShowError { get; private set; }
    public bool IsSubmitting { get; private set; }

    public async Task SubmitAsync()
    {
        IsSubmitting = true;
        ShowError = !await sessionService.LoginAsync(Password);
        IsSubmitting = false;
    }
}

using InvestissementsDashboard.Client.Services;

namespace InvestissementsDashboard.Client.ViewModels;

/// <summary>
/// Presentation state of <c>App.razor</c>: the authentication gate shown before any route is mounted.
/// </summary>
public class AppViewModel(ISessionService sessionService)
{
    public bool IsAuthenticated => sessionService.IsAuthenticated;

    public event Action? OnChange
    {
        add => sessionService.OnChange += value;
        remove => sessionService.OnChange -= value;
    }

    public Task InitializeAsync() => sessionService.InitializeAsync();
}

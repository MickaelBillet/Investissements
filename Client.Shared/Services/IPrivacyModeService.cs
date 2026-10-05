namespace InvestissementsDashboard.Client.Services;

public interface IPrivacyModeService
{
    bool IsHidden { get; }
    event Action? OnChange;
    Task InitializeAsync();
    Task ToggleAsync();
}

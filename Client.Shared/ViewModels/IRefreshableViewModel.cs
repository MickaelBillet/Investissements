namespace InvestissementsDashboard.Client.ViewModels;

/// <summary>
/// A page's ViewModel whose data can be reloaded while the page stays on screen, e.g. after a manual synchronization.
/// </summary>
public interface IRefreshableViewModel
{
    /// <summary>
    /// Reloads the data if it has been loaded at least once, keeping the current data on screen until the new one has
    /// arrived. A ViewModel not loaded yet does nothing: its first load will fetch fresh data anyway.
    /// </summary>
    /// <exception cref="Exception">The data could not be reloaded: the current data is kept and the caller reports it.</exception>
    Task ReloadAsync(CancellationToken ct = default);
}

using Bunit;
using InvestissementsDashboard.Client.Services;
using InvestissementsDashboard.Client.Tests.Helpers;
using InvestissementsDashboard.Client.ViewModels;
using InvestissementsDashboard.Client.Views;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using MudBlazor.Services;

namespace InvestissementsDashboard.Client.Tests.Components;

public class SettingsTests : BunitContext, IAsyncLifetime
{
    // MudBlazor's services only implement IAsyncDisposable: dispose asynchronously.
    public Task InitializeAsync() => Task.CompletedTask;

    Task IAsyncLifetime.DisposeAsync() => base.DisposeAsync().AsTask();

    private readonly Mock<IAgentSettings> _settings = new();
    private readonly Mock<ILocalizationService> _localizer = new();

    public SettingsTests()
    {
        Services.AddMudServices(opt => opt.PopoverOptions.CheckForPopoverProvider = false);
        _localizer.Setup(l => l.Translate(It.IsAny<string>())).Returns<string>(key => TestData.Translate(key));
        Services.AddSingleton(_localizer.Object);
        JSInterop.Mode = JSRuntimeMode.Loose;
    }

    private void RegisterViewModel(bool withSettings)
    {
        Services.AddSingleton(new SettingsViewModel(_localizer.Object, withSettings ? _settings.Object : null));
    }

    [Fact]
    public void Settings_WithoutSettingsStore_RendersNothing()
    {
        RegisterViewModel(withSettings: false);

        var cut = Render<Settings>();

        Assert.DoesNotContain("Enregistrer", cut.Markup);
    }

    [Fact]
    public void Settings_WithStoredEndpoint_ShowsItInTheForm()
    {
        _settings.SetupGet(s => s.FoundryEndpoint).Returns("https://foundry.test/");
        RegisterViewModel(withSettings: true);

        var cut = Render<Settings>();

        Assert.Contains("https://foundry.test/", cut.Markup);
        Assert.Contains("Enregistrer", cut.Markup);
    }

    [Fact]
    public void Settings_SavingInvalidEndpoint_ShowsError()
    {
        RegisterViewModel(withSettings: true);
        var cut = Render<Settings>();

        cut.Find("form").Submit();

        Assert.Contains("URL https valide", cut.Markup);
        _settings.Verify(s => s.Save(It.IsAny<string?>(), It.IsAny<string?>()), Times.Never);
    }
}

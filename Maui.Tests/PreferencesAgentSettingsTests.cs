using InvestissementsDashboard.Maui.Services;
using Moq;
using Xunit;

namespace InvestissementsDashboard.Maui.Tests;

public class PreferencesAgentSettingsTests
{
    private const string DefaultUrl = "https://invest.test/";
    private readonly Mock<IPreferences> _preferences = new();

    private PreferencesAgentSettings Create() => new(_preferences.Object, DefaultUrl);

    private void Stored(string key, string? value) =>
        _preferences.Setup(p => p.Get<string?>(key, null, null)).Returns(value);

    [Fact]
    public void FoundryEndpoint_WhenStored_ReturnsIt()
    {
        Stored(PreferencesAgentSettings.EndpointKey, "https://foundry.test/");

        Assert.Equal("https://foundry.test/", Create().FoundryEndpoint);
    }

    [Fact]
    public void Constructor_WhenBaseUrlNotStored_SeedsTheDefault()
    {
        Create();

        _preferences.Verify(p => p.Set(PreferencesAgentSettings.InvestZaptoBaseUrlKey, DefaultUrl, null), Times.Once);
    }

    [Fact]
    public void Constructor_WhenBaseUrlAlreadyStored_KeepsIt()
    {
        Stored(PreferencesAgentSettings.InvestZaptoBaseUrlKey, "https://custom.test/");

        var settings = Create();

        _preferences.Verify(p => p.Set(PreferencesAgentSettings.InvestZaptoBaseUrlKey, It.IsAny<string>(), null), Times.Never);
        Assert.Equal("https://custom.test/", settings.InvestZaptoBaseUrl);
    }

    [Fact]
    public void Model_WhenBlank_ReturnsNull()
    {
        Stored(PreferencesAgentSettings.ModelKey, "  ");

        Assert.Null(Create().Model);
    }

    [Fact]
    public void Save_WithValues_StoresBoth()
    {
        Create().Save("https://foundry.test/", "gpt-x");

        _preferences.Verify(p => p.Set(PreferencesAgentSettings.EndpointKey, "https://foundry.test/", null), Times.Once);
        _preferences.Verify(p => p.Set(PreferencesAgentSettings.ModelKey, "gpt-x", null), Times.Once);
    }

    [Fact]
    public void Save_WithNullModel_RemovesTheStoredModel()
    {
        Create().Save("https://foundry.test/", null);

        _preferences.Verify(p => p.Remove(PreferencesAgentSettings.ModelKey, null), Times.Once);
        _preferences.Verify(p => p.Set(PreferencesAgentSettings.ModelKey, It.IsAny<string>(), null), Times.Never);
    }
}

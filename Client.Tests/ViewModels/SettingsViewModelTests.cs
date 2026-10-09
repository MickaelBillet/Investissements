using InvestissementsDashboard.Client.Services;
using InvestissementsDashboard.Client.ViewModels;
using Moq;

namespace InvestissementsDashboard.Client.Tests.ViewModels;

public class SettingsViewModelTests
{
    private readonly Mock<ILocalizationService> _localizer = new();
    private readonly Mock<IAgentSettings> _settings = new();

    public SettingsViewModelTests()
    {
        _localizer.Setup(l => l.Translate(It.IsAny<string>())).Returns<string>(key => key);
    }

    private SettingsViewModel Create() => new(_localizer.Object, _settings.Object);

    [Fact]
    public void IsAvailable_WithoutSettingsStore_IsFalse()
    {
        Assert.False(new SettingsViewModel(_localizer.Object).IsAvailable);
    }

    [Fact]
    public void IsAvailable_WithSettingsStore_IsTrue()
    {
        Assert.True(Create().IsAvailable);
    }

    [Fact]
    public void Load_CopiesStoredValues()
    {
        _settings.SetupGet(s => s.FoundryEndpoint).Returns("https://foundry.test/");
        _settings.SetupGet(s => s.Model).Returns("gpt-x");
        var vm = Create();

        vm.Load();

        Assert.Equal("https://foundry.test/", vm.Endpoint);
        Assert.Equal("gpt-x", vm.Model);
    }

    [Fact]
    public void Load_WhenNothingStored_LeavesEmptyValues()
    {
        var vm = Create();

        vm.Load();

        Assert.Equal(string.Empty, vm.Endpoint);
        Assert.Equal(string.Empty, vm.Model);
    }

    [Fact]
    public void Save_WithValidValues_PersistsTrimmedValuesAndReportsSuccess()
    {
        var vm = Create();
        vm.Endpoint = "  https://foundry.test/api/projects/p  ";
        vm.Model = " gpt-x ";

        vm.Save();

        _settings.Verify(s => s.Save("https://foundry.test/api/projects/p", "gpt-x"), Times.Once);
        Assert.False(vm.IsStatusError);
        Assert.Equal("Settings_Saved", vm.StatusMessage);
    }

    [Fact]
    public void Save_WithEmptyModel_StoresNullSoTheDefaultApplies()
    {
        var vm = Create();
        vm.Endpoint = "https://foundry.test/";
        vm.Model = "   ";

        vm.Save();

        _settings.Verify(s => s.Save("https://foundry.test/", null), Times.Once);
    }

    [Theory]
    [InlineData("")]
    [InlineData("not a url")]
    [InlineData("http://foundry.test/")]
    [InlineData("/relative/path")]
    public void Save_WithInvalidEndpoint_DoesNotPersistAndReportsError(string endpoint)
    {
        var vm = Create();
        vm.Endpoint = endpoint;

        vm.Save();

        _settings.Verify(s => s.Save(It.IsAny<string?>(), It.IsAny<string?>()), Times.Never);
        Assert.True(vm.IsStatusError);
        Assert.Equal("Settings_EndpointInvalid", vm.StatusMessage);
    }

    [Fact]
    public void Save_WithoutSettingsStore_Throws()
    {
        var vm = new SettingsViewModel(_localizer.Object);

        Assert.Throws<InvalidOperationException>(() => vm.Save());
    }

    [Fact]
    public void Load_AfterSave_ClearsStatusMessage()
    {
        var vm = Create();
        vm.Endpoint = "https://foundry.test/";
        vm.Save();

        vm.Load();

        Assert.Null(vm.StatusMessage);
    }
}

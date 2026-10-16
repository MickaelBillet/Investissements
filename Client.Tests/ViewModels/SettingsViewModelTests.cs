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

    private readonly Mock<IKeyValueStore> _secrets = new();

    private SettingsViewModel Create() => new(_localizer.Object, _settings.Object, _secrets.Object);

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
    public async Task Save_WithValidValues_PersistsTrimmedValuesAndReportsSuccess()
    {
        var vm = Create();
        vm.Endpoint = "  https://foundry.test/api/projects/p  ";
        vm.Model = " gpt-x ";

        await vm.SaveAsync();

        _settings.Verify(s => s.Save("https://foundry.test/api/projects/p", "gpt-x"), Times.Once);
        Assert.False(vm.IsStatusError);
        Assert.Equal("Settings_Saved", vm.StatusMessage);
    }

    [Fact]
    public async Task Save_WithEmptyModel_StoresNullSoTheDefaultApplies()
    {
        var vm = Create();
        vm.Endpoint = "https://foundry.test/";
        vm.Model = "   ";

        await vm.SaveAsync();

        _settings.Verify(s => s.Save("https://foundry.test/", null), Times.Once);
    }

    [Theory]
    [InlineData("")]
    [InlineData("not a url")]
    [InlineData("http://foundry.test/")]
    [InlineData("/relative/path")]
    public async Task Save_WithInvalidEndpoint_DoesNotPersistAndReportsError(string endpoint)
    {
        var vm = Create();
        vm.Endpoint = endpoint;

        await vm.SaveAsync();

        _settings.Verify(s => s.Save(It.IsAny<string?>(), It.IsAny<string?>()), Times.Never);
        Assert.True(vm.IsStatusError);
        Assert.Equal("Settings_EndpointInvalid", vm.StatusMessage);
    }

    [Fact]
    public async Task Save_WithoutSettingsStore_Throws()
    {
        var vm = new SettingsViewModel(_localizer.Object);

        await Assert.ThrowsAsync<InvalidOperationException>(() => vm.SaveAsync());
    }

    [Fact]
    public async Task Load_AfterSave_ClearsStatusMessage()
    {
        var vm = Create();
        vm.Endpoint = "https://foundry.test/";
        await vm.SaveAsync();

        vm.Load();

        Assert.Null(vm.StatusMessage);
    }

    [Fact]
    public async Task SaveAsync_WithMcpKey_StoresTrimmedKeyAndClearsField()
    {
        var vm = Create();
        vm.Endpoint = "https://foundry.test/";
        vm.McpApiKey = "  secret  ";

        await vm.SaveAsync();

        _secrets.Verify(s => s.SetAsync(AgentSecretKeys.McpApiKey, "secret"), Times.Once);
        Assert.Equal(string.Empty, vm.McpApiKey);
    }

    [Fact]
    public async Task SaveAsync_WithBlankMcpKey_KeepsStoredKey()
    {
        var vm = Create();
        vm.Endpoint = "https://foundry.test/";
        vm.McpApiKey = "   ";

        await vm.SaveAsync();

        _secrets.Verify(s => s.SetAsync(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
        _secrets.Verify(s => s.RemoveAsync(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task SaveAsync_WithInvalidEndpoint_DoesNotStoreMcpKey()
    {
        var vm = Create();
        vm.Endpoint = "not a url";
        vm.McpApiKey = "secret";

        await vm.SaveAsync();

        _secrets.Verify(s => s.SetAsync(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public void Load_NeverExposesStoredMcpKey()
    {
        var vm = Create();
        vm.McpApiKey = "typed";

        vm.Load();

        Assert.Equal(string.Empty, vm.McpApiKey);
        _secrets.Verify(s => s.GetAsync(It.IsAny<string>()), Times.Never);
    }
}

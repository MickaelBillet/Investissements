using InvestissementsDashboard.Client.Services;
using InvestissementsDashboard.Client.ViewModels;
using InvestissementsDashboard.Shared.Models;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace InvestissementsDashboard.Client.Tests.ViewModels;

public class MainViewModelTests
{
    private readonly Mock<ISyncService> _sync = new();
    private readonly Mock<ISessionService> _session = new();
    private readonly Mock<ILocalizationService> _localizer = new();

    public MainViewModelTests()
    {
        _localizer.Setup(l => l.Translate(It.IsAny<string>())).Returns<string>(key => key);
    }

    private MainViewModel Create() => new(_sync.Object, _session.Object, _localizer.Object, NullLogger<MainViewModel>.Instance);

    private void SetupResult(SyncResultDto result) =>
        _sync.Setup(s => s.TriggerAsync(It.IsAny<CancellationToken>())).ReturnsAsync(result);

    [Fact]
    public async Task SyncAsync_WhenAssetsAdded_SetsSuccessMessageWithCount()
    {
        _localizer.Setup(l => l.Translate("Sync_Success_WithAdded")).Returns("{0} ajouté(s)");
        SetupResult(new SyncResultDto(true, 3, null));
        var vm = Create();

        await vm.SyncAsync();

        Assert.True(vm.IsSyncSuccess);
        Assert.Equal("3 ajouté(s)", vm.SyncMessage);
        Assert.False(vm.IsSyncing);
    }

    [Fact]
    public async Task SyncAsync_WhenNothingAdded_SetsSuccessMessageWithoutCount()
    {
        SetupResult(new SyncResultDto(true, 0, null));
        var vm = Create();

        await vm.SyncAsync();

        Assert.True(vm.IsSyncSuccess);
        Assert.Equal("Sync_Success_NoAdded", vm.SyncMessage);
    }

    [Fact]
    public async Task SyncAsync_WhenApiReportsErrorMessage_AppendsItToErrorMessage()
    {
        SetupResult(new SyncResultDto(false, 0, "Erreur"));
        var vm = Create();

        await vm.SyncAsync();

        Assert.False(vm.IsSyncSuccess);
        Assert.Equal("Sync_Error — Erreur", vm.SyncMessage);
        Assert.False(vm.IsSyncing);
    }

    [Fact]
    public async Task SyncAsync_WhenApiReportsNoErrorMessage_SetsGenericErrorMessage()
    {
        SetupResult(new SyncResultDto(false, 0, null));
        var vm = Create();

        await vm.SyncAsync();

        Assert.False(vm.IsSyncSuccess);
        Assert.Equal("Sync_Error", vm.SyncMessage);
    }

    [Fact]
    public async Task SyncAsync_WhenServiceThrows_SetsErrorMessageAndResetsIsSyncing()
    {
        _sync.Setup(s => s.TriggerAsync(It.IsAny<CancellationToken>()))
             .ThrowsAsync(new HttpRequestException("réseau"));
        var vm = Create();

        await vm.SyncAsync();

        Assert.False(vm.IsSyncSuccess);
        Assert.Equal("Sync_Error — réseau", vm.SyncMessage);
        Assert.False(vm.IsSyncing);
    }

    [Fact]
    public async Task SyncAsync_WhenCancelled_Propagates()
    {
        _sync.Setup(s => s.TriggerAsync(It.IsAny<CancellationToken>()))
             .ThrowsAsync(new OperationCanceledException());
        var vm = Create();

        await Assert.ThrowsAsync<OperationCanceledException>(() => vm.SyncAsync());

        Assert.False(vm.IsSyncing);
    }

    [Fact]
    public async Task LogoutAsync_CallsSessionLogout()
    {
        await Create().LogoutAsync();

        _session.Verify(s => s.LogoutAsync(), Times.Once);
    }

    [Fact]
    public void SyncMessage_BeforeAnySync_IsNull()
    {
        Assert.Null(Create().SyncMessage);
    }
}

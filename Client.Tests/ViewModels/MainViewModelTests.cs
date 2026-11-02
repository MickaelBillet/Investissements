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

    private readonly Mock<IRefreshableViewModel> _dashboard = new();
    private readonly Mock<IRefreshableViewModel> _tracking = new();

    private MainViewModel Create() => new(_sync.Object, _session.Object, _localizer.Object, NullLogger<MainViewModel>.Instance, [_dashboard.Object, _tracking.Object]);

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
    public void IsAnalysisAvailable_WithoutRunner_IsFalse()
    {
        Assert.False(Create().IsAnalysisAvailable);
    }

    [Fact]
    public void IsAnalysisAvailable_WithRunner_IsTrue()
    {
        var vm = new MainViewModel(_sync.Object, _session.Object, _localizer.Object, NullLogger<MainViewModel>.Instance, [], reviewRunner: new Mock<IPortfolioReviewRunner>().Object);

        Assert.True(vm.IsAnalysisAvailable);
    }

    [Fact]
    public void SyncMessage_BeforeAnySync_IsNull()
    {
        Assert.Null(Create().SyncMessage);
    }

    [Fact]
    public async Task SyncAsync_WhenSyncSucceeds_ReloadsEveryPage()
    {
        SetupResult(new SyncResultDto(true, 0, null));

        await Create().SyncAsync();

        _dashboard.Verify(v => v.ReloadAsync(It.IsAny<CancellationToken>()), Times.Once);
        _tracking.Verify(v => v.ReloadAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task SyncAsync_WhenSyncFails_DoesNotReload()
    {
        SetupResult(new SyncResultDto(false, 0, "refusé"));

        await Create().SyncAsync();

        _dashboard.Verify(v => v.ReloadAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task SyncAsync_WhileReloading_StaysSyncingAndReportsOnlyAtTheEnd()
    {
        SetupResult(new SyncResultDto(true, 0, null));
        var reload = new TaskCompletionSource();
        _dashboard.Setup(v => v.ReloadAsync(It.IsAny<CancellationToken>())).Returns(reload.Task);
        var vm = Create();

        var sync = vm.SyncAsync();
        Assert.True(vm.IsSyncing);
        reload.SetResult();
        await sync;

        Assert.False(vm.IsSyncing);
        Assert.True(vm.IsSyncSuccess);
    }

    [Fact]
    public async Task SyncAsync_WhenReloadFails_ReportsTheRefreshError()
    {
        SetupResult(new SyncResultDto(true, 0, null));
        _dashboard.Setup(v => v.ReloadAsync(It.IsAny<CancellationToken>())).ThrowsAsync(new HttpRequestException("indisponible"));
        var vm = Create();

        await vm.SyncAsync();

        Assert.False(vm.IsSyncSuccess);
        Assert.Equal("Sync_RefreshError — indisponible", vm.SyncMessage);
        Assert.False(vm.IsSyncing);
    }
}

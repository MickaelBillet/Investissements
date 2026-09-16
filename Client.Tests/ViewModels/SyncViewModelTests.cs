using InvestissementsDashboard.Client.Services;
using InvestissementsDashboard.Client.ViewModels;
using InvestissementsDashboard.Shared.Models;
using Moq;

namespace InvestissementsDashboard.Client.Tests.ViewModels;

public class SyncViewModelTests
{
    [Fact]
    public async Task TriggerAsync_WhenSyncSucceeds_ReturnsResultAndResetsIsSyncing()
    {
        var mock = new Mock<ISyncService>();
        mock.Setup(s => s.TriggerAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new SyncResultDto(true, 3, null));
        var vm = new SyncViewModel(mock.Object);

        var result = await vm.TriggerAsync();

        Assert.True(result.Success);
        Assert.Equal(3, result.AddedCount);
        Assert.False(vm.IsSyncing);
    }

    [Fact]
    public async Task TriggerAsync_WhenSyncFails_ReturnsResultAndResetsIsSyncing()
    {
        var mock = new Mock<ISyncService>();
        mock.Setup(s => s.TriggerAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new SyncResultDto(false, 0, "Erreur"));
        var vm = new SyncViewModel(mock.Object);

        var result = await vm.TriggerAsync();

        Assert.False(result.Success);
        Assert.Equal("Erreur", result.ErrorMessage);
        Assert.False(vm.IsSyncing);
    }

    [Fact]
    public async Task TriggerAsync_WhenServiceThrows_ResetsIsSyncingAndPropagatesException()
    {
        var mock = new Mock<ISyncService>();
        mock.Setup(s => s.TriggerAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new HttpRequestException());
        var vm = new SyncViewModel(mock.Object);

        await Assert.ThrowsAsync<HttpRequestException>(() => vm.TriggerAsync());

        Assert.False(vm.IsSyncing);
    }
}

using Bunit;
using InvestissementsDashboard.Client.Services;
using InvestissementsDashboard.Client.Tests.Helpers;
using InvestissementsDashboard.Client.ViewModels;
using InvestissementsDashboard.Client.Views;
using InvestissementsDashboard.Shared.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using MudBlazor.Services;

namespace InvestissementsDashboard.Client.Tests.Components;

/// <summary>
/// A reload runs outside the page's own lifecycle: only the page's subscription to DataChanged re-renders it. Each test
/// starts on a failed first load, whose error message is visible, so that its disappearance proves a new render.
/// </summary>
public class RefreshAfterSyncTests : BunitContext, IAsyncLifetime
{
    // MudBlazor's services only implement IAsyncDisposable: dispose asynchronously.
    public Task InitializeAsync() => Task.CompletedTask;

    Task IAsyncLifetime.DisposeAsync() => base.DisposeAsync().AsTask();

    private const string Failure = "service indisponible";

    private readonly Mock<IPortfolioService> _portfolio = new();

    public RefreshAfterSyncTests()
    {
        Services.AddMudServices(opt => opt.PopoverOptions.CheckForPopoverProvider = false);
        Services.AddLocalizationMock();
        Services.AddPrivacyModeMock();
        JSInterop.Mode = JSRuntimeMode.Loose;
    }

    private TrackingViewModel RegisterTracking()
    {
        Services.AddSingleton(sp => new TrackingViewModel(_portfolio.Object, sp.GetRequiredService<ILocalizationService>()));
        return Services.GetRequiredService<TrackingViewModel>();
    }

    [Fact]
    public async Task Tracking_AfterReload_RendersTheNewData()
    {
        _portfolio.SetupSequence(s => s.GetIndexedHistoryAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new HttpRequestException(Failure))
            .ReturnsAsync(Array.Empty<PerformancePointDto>());
        _portfolio.Setup(s => s.GetBondScheduleAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<BondScheduleDto>());
        var viewModel = RegisterTracking();
        var cut = Render<Tracking>();
        cut.WaitForAssertion(() => Assert.Contains(Failure, cut.Markup));

        await cut.InvokeAsync(() => viewModel.ReloadAsync());

        cut.WaitForAssertion(() => Assert.DoesNotContain(Failure, cut.Markup));
    }

    [Fact]
    public async Task Dashboard_AfterReload_RendersTheNewData()
    {
        _portfolio.SetupSequence(s => s.GetAssetsAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new HttpRequestException(Failure))
            .ReturnsAsync(Array.Empty<AssetDto>());
        _portfolio.Setup(s => s.GetSnapshotHistoryAsync(It.IsAny<CancellationToken>())).ReturnsAsync(Array.Empty<SnapshotDto>());
        _portfolio.Setup(s => s.GetIndexedHistoryAsync(It.IsAny<CancellationToken>())).ReturnsAsync(Array.Empty<PerformancePointDto>());
        _portfolio.Setup(s => s.GetAssetTypeReferenceAsync(It.IsAny<CancellationToken>())).ReturnsAsync(Array.Empty<AssetTypeReferenceDto>());
        _portfolio.Setup(s => s.GetGeographyDistributionAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(Array.Empty<DistributionDto>());
        _portfolio.Setup(s => s.GetSectorDistributionAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(Array.Empty<DistributionDto>());
        Services.AddSingleton(sp =>
        {
            var localizer = sp.GetRequiredService<ILocalizationService>();
            return new DashboardViewModel(_portfolio.Object, localizer, new PanelDistributionService(localizer), NullLogger<DashboardViewModel>.Instance);
        });
        var viewModel = Services.GetRequiredService<DashboardViewModel>();
        var cut = Render<Dashboard>();
        cut.WaitForAssertion(() => Assert.Contains(Failure, cut.Markup));

        await cut.InvokeAsync(() => viewModel.ReloadAsync());

        cut.WaitForAssertion(() => Assert.DoesNotContain(Failure, cut.Markup));
    }

    [Fact]
    public async Task Tracking_AfterDispose_NoLongerRendersOnReload()
    {
        _portfolio.Setup(s => s.GetIndexedHistoryAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<PerformancePointDto>());
        _portfolio.Setup(s => s.GetBondScheduleAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<BondScheduleDto>());
        var viewModel = RegisterTracking();
        var cut = Render<Tracking>();
        var rendersBefore = cut.RenderCount;

        cut.Instance.Dispose();
        await cut.InvokeAsync(() => viewModel.ReloadAsync());

        Assert.Equal(rendersBefore, cut.RenderCount);
    }
}

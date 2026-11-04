using InvestissementsDashboard.Client.Services;
using InvestissementsDashboard.Maui.Services;
using InvestissementsDashboard.Shared.Models;
using Moq;
using Xunit;

namespace InvestissementsDashboard.Maui.Tests;

public class AllocationSourceTests
{
    private readonly Mock<IPortfolioService> _portfolio = new();

    private AllocationSource Create() => new(_portfolio.Object);

    [Fact]
    public async Task GetWatchlistAsync_Always_ReturnsPortfolioServiceWatchlist()
    {
        IReadOnlyList<WatchlistItemDto> watchlist = [new("ASML", "NL0010273215")];
        _portfolio.Setup(p => p.GetWatchlistAsync(It.IsAny<CancellationToken>())).ReturnsAsync(watchlist);

        Assert.Same(watchlist, await Create().GetWatchlistAsync(CancellationToken.None));
    }

    [Fact]
    public async Task GetAssetsAsync_Always_ReturnsPortfolioServiceAssets()
    {
        IReadOnlyList<AssetDto> assets = [];
        _portfolio.Setup(p => p.GetAssetsAsync(It.IsAny<CancellationToken>())).ReturnsAsync(assets);

        Assert.Same(assets, await Create().GetAssetsAsync(CancellationToken.None));
    }

    [Fact]
    public async Task GetLastSnapshotAsync_Always_ReturnsPortfolioServiceSnapshot()
    {
        var snapshot = new SnapshotDto(new DateOnly(2026, 11, 4), 1m, 1m, 1m, 1m, 1m, 1m);
        _portfolio.Setup(p => p.GetLastSnapshotAsync(It.IsAny<CancellationToken>())).ReturnsAsync(snapshot);

        Assert.Same(snapshot, await Create().GetLastSnapshotAsync(CancellationToken.None));
    }

    [Fact]
    public async Task GetAssetsAsync_PortfolioServiceThrows_Propagates()
    {
        _portfolio.Setup(p => p.GetAssetsAsync(It.IsAny<CancellationToken>())).ThrowsAsync(new HttpRequestException("Api unreachable"));

        await Assert.ThrowsAsync<HttpRequestException>(() => Create().GetAssetsAsync(CancellationToken.None));
    }
}

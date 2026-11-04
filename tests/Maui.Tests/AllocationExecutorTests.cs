using AgentAI.Core.Workflows.PortfolioReview;
using AgentAI.Core.Workflows.PortfolioReview.Events;
using AgentAI.Core.Workflows.PortfolioReview.Executors;
using AgentAI.Core.Workflows.PortfolioReview.Messages;
using InvestissementsDashboard.Shared.Models;
using Microsoft.Agents.AI.Workflows;
using Moq;
using Xunit;

namespace InvestissementsDashboard.Maui.Tests;

public class AllocationExecutorTests
{
    private static readonly DateOnly RequestDate = new(2026, 10, 19);

    private readonly Mock<IAllocationSource> _source = new();
    private readonly Mock<IWorkflowContext> _context = new();

    public AllocationExecutorTests()
    {
        _source.Setup(s => s.GetAssetsAsync(It.IsAny<CancellationToken>()))
               .ReturnsAsync([Asset("Livret A", 500m, 25m), Asset("MSCI World", 1_500m, 75m, isin: "IE00B4L5Y983", gain: 100m, roi: 7m)]);
        _source.Setup(s => s.GetLastSnapshotAsync(It.IsAny<CancellationToken>())).ReturnsAsync(Snapshot(new DateOnly(2026, 10, 18)));
        _source.Setup(s => s.GetWatchlistAsync(It.IsAny<CancellationToken>())).ReturnsAsync([new WatchlistItemDto("ASML", "NL0010273215")]);
    }

    private static AssetDto Asset(string name, decimal? total, decimal weight, string isin = "", decimal? gain = null, decimal? roi = null) =>
        new(1, name, "Stocks", "PEA", "PEA TR", "ETF_Stocks", "Diversified", "", "World", 2, 100m, 0m, 0m, total, gain, null, roi, weight, isin);

    private static SnapshotDto Snapshot(DateOnly date) => new(date, 82_000m, 31m, 161m, 118_000m, 3_000m, 35_000m);

    private ValueTask<AllocationResult> HandleAsync() =>
        new AllocationExecutor(_source.Object).HandleAsync(new AnalysisRequest(RequestDate), _context.Object);

    [Fact]
    public async Task HandleAsync_AssetsRead_MapsEveryFieldOfTheLine()
    {
        var result = await HandleAsync();

        Assert.Equal(new AllocationLine("MSCI World", "IE00B4L5Y983", "Stocks", "Diversified", "World", 1_500m, 75m, 100m, 7m), result.Lines[0]);
    }

    [Fact]
    public async Task HandleAsync_AssetsRead_SortsByDescendingValue()
    {
        var result = await HandleAsync();

        Assert.Equal(["MSCI World", "Livret A"], result.Lines.Select(l => l.Name));
    }

    [Fact]
    public async Task HandleAsync_AssetsRead_TotalIsTheSumOfThePositionsNotTheNetCapital()
    {
        var result = await HandleAsync();

        Assert.Equal(2_000m, result.TotalValue);
    }

    [Fact]
    public async Task HandleAsync_EveryPosition_IsKeptExactlyOnce()
    {
        _source.Setup(s => s.GetAssetsAsync(It.IsAny<CancellationToken>()))
               .ReturnsAsync(Enumerable.Range(1, 200).Select(i => Asset($"Ligne {i}", i, 1m)).ToList());

        var result = await HandleAsync();

        Assert.Equal(200, result.Lines.Count);
        Assert.Equal(200, result.Lines.Select(l => l.Name).Distinct().Count());
    }

    [Theory]
    [InlineData(null, null)]
    [InlineData(0, null)]
    [InlineData(0, 0)]
    [InlineData(0, 12)]
    public async Task HandleAsync_NoValueAndNoLoss_IsIgnored(int? total, int? roi)
    {
        _source.Setup(s => s.GetAssetsAsync(It.IsAny<CancellationToken>())).ReturnsAsync([Asset("Soldée", total, 0m, roi: roi), Asset("MSCI World", 1_500m, 100m)]);

        var result = await HandleAsync();

        Assert.Equal("MSCI World", Assert.Single(result.Lines).Name);
        Assert.Equal(1_500m, result.TotalValue);
    }

    [Theory]
    [InlineData(null)]
    [InlineData(0)]
    public async Task HandleAsync_LostPosition_IsKeptWithoutAddingToTheTotal(int? total)
    {
        _source.Setup(s => s.GetAssetsAsync(It.IsAny<CancellationToken>())).ReturnsAsync([Asset("Projet en défaut", total, 0m, gain: -70m, roi: -100m), Asset("MSCI World", 1_500m, 100m)]);

        var result = await HandleAsync();

        var lost = result.Lines.Single(l => l.Name == "Projet en défaut");
        Assert.Equal(0m, lost.Value);
        Assert.Equal(-70m, lost.UnrealizedGain);
        Assert.Equal(-100m, lost.Roi);
        Assert.Equal(1_500m, result.TotalValue);
    }

    [Fact]
    public async Task HandleAsync_UnknownGainAndEmptyIsin_StayDistinctFromZero()
    {
        _source.Setup(s => s.GetAssetsAsync(It.IsAny<CancellationToken>())).ReturnsAsync([Asset("Sans plus-value", 100m, 100m)]);

        var line = Assert.Single((await HandleAsync()).Lines);

        Assert.Null(line.Isin);
        Assert.Null(line.UnrealizedGain);
        Assert.Null(line.Roi);
    }

    [Fact]
    public async Task HandleAsync_WatchlistRead_StaysOutOfTheLinesAndOfTheTotal()
    {
        var result = await HandleAsync();

        Assert.Equal(new WatchlistItemDto("ASML", "NL0010273215"), Assert.Single(result.Watchlist));
        Assert.DoesNotContain(result.Lines, l => l.Name == "ASML");
        Assert.Equal(2_000m, result.TotalValue);
    }

    [Fact]
    public async Task HandleAsync_SnapshotRead_UsesTheDateOfTheData()
    {
        Assert.Equal(new DateOnly(2026, 10, 18), (await HandleAsync()).Date);
    }

    [Fact]
    public async Task HandleAsync_NoSnapshot_FallsBackOnTheRequestedDate()
    {
        _source.Setup(s => s.GetLastSnapshotAsync(It.IsAny<CancellationToken>())).ReturnsAsync((SnapshotDto?)null);

        Assert.Equal(RequestDate, (await HandleAsync()).Date);
    }

    [Fact]
    public async Task HandleAsync_DataRead_EmitsAllocationRetrievedEvent()
    {
        await HandleAsync();

        _context.Verify(c => c.AddEventAsync(It.IsAny<AllocationRetrievedEvent>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task HandleAsync_WatchlistUnavailable_Propagates()
    {
        _source.Setup(s => s.GetWatchlistAsync(It.IsAny<CancellationToken>())).ThrowsAsync(new HttpRequestException("Api unreachable"));

        await Assert.ThrowsAsync<HttpRequestException>(() => HandleAsync().AsTask());
    }
}

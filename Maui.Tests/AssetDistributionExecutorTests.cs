using AgentAI;
using InvestissementsDashboard.Shared.Models;
using Microsoft.Agents.AI.Workflows;
using Moq;
using Xunit;

namespace InvestissementsDashboard.Maui.Tests;

public class AssetDistributionExecutorTests
{
    private static readonly AnalysisRequest Request = new(new DateOnly(2026, 10, 22));

    private readonly Mock<IAssetDistributionSource> _source = new();

    public AssetDistributionExecutorTests()
    {
        _source.Setup(s => s.GetGeographyAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((string assetClass, CancellationToken _) => [new($"{assetClass}-zone", 1_000m, 100m)]);
        _source.Setup(s => s.GetSectorsAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((string assetClass, CancellationToken _) => [new($"{assetClass}-sector", 1_000m, 100m)]);
    }

    private readonly Mock<IWorkflowContext> _context = new();

    private Task<DistributionResult> HandleAsync(CancellationToken cancellationToken = default) =>
        new AssetDistributionExecutor(_source.Object).HandleAsync(Request, _context.Object, cancellationToken).AsTask();

    [Fact]
    public async Task HandleAsync_Request_PublishesTheDistributionsAsAnEvent()
    {
        var result = await HandleAsync();

        _context.Verify(c => c.AddEventAsync(
            It.Is<AssetDistributionRetrievedEvent>(e => ReferenceEquals(e.Data, result)), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task HandleAsync_Request_ReturnsStocksThenBonds()
    {
        var result = await HandleAsync();

        Assert.Equal(["Stocks", "Bonds"], result.Classes.Select(c => c.AssetClass));
    }

    [Fact]
    public async Task HandleAsync_Request_PairsEachClassWithItsOwnGeographyAndSectors()
    {
        var result = await HandleAsync();

        var stocks = result.Classes.Single(c => c.AssetClass == "Stocks");
        Assert.Equal("Stocks-zone", Assert.Single(stocks.Geography).Name);
        Assert.Equal("Stocks-sector", Assert.Single(stocks.Sectors).Name);
        var bonds = result.Classes.Single(c => c.AssetClass == "Bonds");
        Assert.Equal("Bonds-zone", Assert.Single(bonds.Geography).Name);
        Assert.Equal("Bonds-sector", Assert.Single(bonds.Sectors).Name);
    }

    [Fact]
    public async Task HandleAsync_Request_PassesTheCancellationTokenToEverySourceCall()
    {
        using var cts = new CancellationTokenSource();

        await HandleAsync(cts.Token);

        _source.Verify(s => s.GetGeographyAsync(It.IsAny<string>(), cts.Token), Times.Exactly(2));
        _source.Verify(s => s.GetSectorsAsync(It.IsAny<string>(), cts.Token), Times.Exactly(2));
    }

    [Fact]
    public async Task HandleAsync_EmptyDistributions_ReturnsClassesWithEmptyLists()
    {
        _source.Setup(s => s.GetGeographyAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync([]);
        _source.Setup(s => s.GetSectorsAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync([]);

        var result = await HandleAsync();

        Assert.Equal(2, result.Classes.Count);
        Assert.All(result.Classes, c => Assert.Empty(c.Geography));
        Assert.All(result.Classes, c => Assert.Empty(c.Sectors));
    }

    [Fact]
    public async Task HandleAsync_SourceThrows_PropagatesException()
    {
        _source.Setup(s => s.GetSectorsAsync("Bonds", It.IsAny<CancellationToken>()))
            .ThrowsAsync(new HttpRequestException("Api unreachable"));

        await Assert.ThrowsAsync<HttpRequestException>(() => HandleAsync());
    }
}

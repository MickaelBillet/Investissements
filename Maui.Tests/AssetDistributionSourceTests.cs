using InvestissementsDashboard.Client.Services;
using InvestissementsDashboard.Maui.Services;
using InvestissementsDashboard.Shared.Models;
using Moq;

namespace InvestissementsDashboard.Maui.Tests;

public class AssetDistributionSourceTests
{
    [Fact]
    public async Task GetGeographyAsync_ValidAssetClass_ReturnsPortfolioServiceGeography()
    {
        DistributionDto[] geography = [new("USA", 1_000m, 100m)];
        var portfolio = new Mock<IPortfolioService>();
        portfolio.Setup(p => p.GetGeographyDistributionAsync("Stocks", It.IsAny<CancellationToken>())).ReturnsAsync(geography);

        var result = await new AssetDistributionSource(portfolio.Object).GetGeographyAsync("Stocks", CancellationToken.None);

        Assert.Same(geography, result);
    }

    [Fact]
    public async Task GetSectorsAsync_ValidAssetClass_ReturnsPortfolioServiceSectors()
    {
        DistributionDto[] sectors = [new("Sovereign", 2_000m, 100m)];
        var portfolio = new Mock<IPortfolioService>();
        portfolio.Setup(p => p.GetSectorDistributionAsync("Bonds", It.IsAny<CancellationToken>())).ReturnsAsync(sectors);

        var result = await new AssetDistributionSource(portfolio.Object).GetSectorsAsync("Bonds", CancellationToken.None);

        Assert.Same(sectors, result);
    }

    [Fact]
    public async Task GetSectorsAsync_PortfolioServiceThrows_PropagatesException()
    {
        var portfolio = new Mock<IPortfolioService>();
        portfolio.Setup(p => p.GetSectorDistributionAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new HttpRequestException("Api unreachable"));

        await Assert.ThrowsAsync<HttpRequestException>(
            () => new AssetDistributionSource(portfolio.Object).GetSectorsAsync("Stocks", CancellationToken.None));
    }
}

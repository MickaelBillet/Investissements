using InvestissementsDashboard.Api.Functions;
using InvestissementsDashboard.Api.Services;
using InvestissementsDashboard.Shared.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace InvestissementsDashboard.Api.Tests.Functions;

public class SectorFunctionTests
{
    private static SectorFunction CreateFunction(Mock<ISectorService> mock)
        => new(mock.Object, NullLogger<SectorFunction>.Instance);

    private static HttpRequest MockRequest() => new Mock<HttpRequest>().Object;

    [Theory]
    [InlineData("Cash")]
    [InlineData("stocks")]
    [InlineData("")]
    public async Task GetSectorDistribution_InvalidAssetClass_ReturnsBadRequest(string assetClass)
    {
        var mock = new Mock<ISectorService>();

        var result = await CreateFunction(mock).GetSectorDistribution(MockRequest(), assetClass, CancellationToken.None);

        var badRequest = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal("Invalid parameter.", badRequest.Value);
        mock.Verify(s => s.GetDistributionAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Theory]
    [InlineData("Stocks")]
    [InlineData("Bonds")]
    public async Task GetSectorDistribution_ValidAssetClass_ReturnsOkWithDistribution(string assetClass)
    {
        DistributionDto[] distribution = [new("Technology", 1_000m, 100m)];
        var mock = new Mock<ISectorService>();
        mock.Setup(s => s.GetDistributionAsync(assetClass, It.IsAny<CancellationToken>())).ReturnsAsync(distribution);

        var result = await CreateFunction(mock).GetSectorDistribution(MockRequest(), assetClass, CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.Same(distribution, ok.Value);
    }

    [Fact]
    public async Task GetSectorDistribution_WhenServiceThrowsHttpRequestException_ReturnsBadGateway()
    {
        var mock = new Mock<ISectorService>();
        mock.Setup(s => s.GetDistributionAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new HttpRequestException("Sheets unreachable"));

        var result = await CreateFunction(mock).GetSectorDistribution(MockRequest(), "Stocks", CancellationToken.None);

        var status = Assert.IsType<StatusCodeResult>(result);
        Assert.Equal(StatusCodes.Status502BadGateway, status.StatusCode);
    }

    [Fact]
    public async Task GetSectorDistribution_WhenServiceThrowsUnexpectedException_ReturnsInternalServerError()
    {
        var mock = new Mock<ISectorService>();
        mock.Setup(s => s.GetDistributionAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("boom"));

        var result = await CreateFunction(mock).GetSectorDistribution(MockRequest(), "Stocks", CancellationToken.None);

        var status = Assert.IsType<StatusCodeResult>(result);
        Assert.Equal(StatusCodes.Status500InternalServerError, status.StatusCode);
    }
}

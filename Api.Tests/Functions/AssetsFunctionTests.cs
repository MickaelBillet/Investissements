using InvestissementsDashboard.Api.Functions;
using InvestissementsDashboard.Api.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace InvestissementsDashboard.Api.Tests.Functions;

public class AssetsFunctionTests
{
    private static AssetsFunction CreateFunction(Mock<IAssetsService> mock)
        => new(mock.Object, NullLogger<AssetsFunction>.Instance);

    private static HttpRequest MockRequest() => new Mock<HttpRequest>().Object;

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa01")] // 101 chars
    public async Task GetByAssetTypeAndInformation_InvalidInformation_ReturnsBadRequest(string information)
    {
        var mock = new Mock<IAssetsService>();

        var result = await CreateFunction(mock).GetByAssetTypeAndInformation(MockRequest(), information, CancellationToken.None);

        var badRequest = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal("Invalid parameter.", badRequest.Value);
        mock.Verify(s => s.GetByAssetTypeAndInformationAsync(
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task GetByAssetTypeAndInformation_ValidInformation_CallsService()
    {
        var mock = new Mock<IAssetsService>();
        mock.Setup(s => s.GetByAssetTypeAndInformationAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        var result = await CreateFunction(mock).GetByAssetTypeAndInformation(MockRequest(), "World", CancellationToken.None);

        Assert.IsType<OkObjectResult>(result);
        mock.Verify(s => s.GetByAssetTypeAndInformationAsync("ETF_Stocks", "World", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetAssetsDistribution_WhenServiceThrowsArgumentException_ReturnsBadRequestWithGenericMessage()
    {
        var mock = new Mock<IAssetsService>();
        mock.Setup(s => s.GetDistributionByDimensionAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new ArgumentException("Unknown dimension 'xyz'. Valid values: assetClass, assetType, support, supportType."));

        var result = await CreateFunction(mock).GetAssetsDistribution(MockRequest(), "xyz", CancellationToken.None);

        var badRequest = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal("Invalid parameter.", badRequest.Value);
    }
}

using InvestissementsDashboard.Api.Services;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace InvestissementsDashboard.Api.Tests.Services;

public class AssetsServiceTests
{
    private static readonly IReadOnlyList<string> HeaderRow =
        ["Id", "Name", "AssetClass", "SupportType", "Support", "AssetType", "Information", "Risk",
         "TotalPurchases", "TotalSales", "Dividends", "CurrentTotal"];

    private static IReadOnlyList<string> MakeRow(
        int id, string name, string assetClass, string supportType, string support, string assetType,
        string info, int risk, string purchases, string sales, string dividends, string currentTotal)
        => [id.ToString(), name, assetClass, supportType, support, assetType, info, risk.ToString(),
            purchases, sales, dividends, currentTotal];

    private static readonly IReadOnlyList<string> RefHeader = ["Id", "Name"];

    private static IReadOnlyList<string> MakeRefRow(int id, string name) => [id.ToString(), name];

    private static AssetsService CreateService(
        IReadOnlyList<IReadOnlyList<string>> assetRows,
        Dictionary<string, IReadOnlyList<IReadOnlyList<string>>>? refRanges = null)
    {
        var mock = new Mock<IGoogleSheetsService>();
        mock.Setup(s => s.GetRangeAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);
        mock.Setup(s => s.GetRangeAsync("Asset", It.IsAny<CancellationToken>()))
            .ReturnsAsync(assetRows);
        foreach (var (range, rows) in refRanges ?? [])
            mock.Setup(s => s.GetRangeAsync(range, It.IsAny<CancellationToken>()))
                .ReturnsAsync(rows);
        return new AssetsService(mock.Object, NullLogger<AssetsService>.Instance);
    }

    [Fact]
    public async Task GetAllAsync_WhenSheetHasRows_ReturnsAllValidAssets()
    {
        var rows = new[] { HeaderRow,
            MakeRow(1, "MSCI World", "Stocks", "PEA", "PEA TR", "ETF_Stocks", "", 4, "5000", "0", "0", "6000"),
            MakeRow(2, "Livret A",   "Cash",   "Booklet", "Livret A", "Savings", "", 0, "3000", "0", "0", "3000")
        };
        var service = CreateService(rows);

        var result = await service.GetAllAsync();

        Assert.Equal(2, result.Count);
        Assert.Equal("MSCI World", result[0].Name);
        Assert.Equal("Livret A", result[1].Name);
    }

    [Fact]
    public async Task GetAllAsync_WhenRowIsNotDefined_SkipsRow()
    {
        var rows = new[] { HeaderRow,
            MakeRow(1, "MSCI World",  "Stocks", "PEA", "PEA TR", "ETF_Stocks", "", 4, "5000", "0", "0", "6000"),
            MakeRow(2, "Not Defined", "Stocks", "PEA", "PEA TR", "ETF_Stocks", "", 4, "ND",   "ND","ND","0")
        };
        var service = CreateService(rows);

        var result = await service.GetAllAsync();

        Assert.Single(result);
        Assert.Equal("MSCI World", result[0].Name);
    }

    [Fact]
    public async Task GetAllAsync_WhenPurchasesIsNd_SetsFinancialFieldsToNull()
    {
        var rows = new[] { HeaderRow,
            MakeRow(1, "P2P Loan", "PrivateDebt", "Platform", "Mintos", "Direct loans (P2P)", "", 3, "ND", "ND", "ND", "2000")
        };
        var service = CreateService(rows);

        var result = await service.GetAllAsync();

        Assert.Single(result);
        Assert.Null(result[0].TotalPurchases);
        Assert.Null(result[0].TotalSales);
        Assert.Null(result[0].Dividends);
        Assert.Null(result[0].UnrealizedGain);
        Assert.Null(result[0].Yield);
        Assert.Null(result[0].Roi);
    }

    [Fact]
    public async Task GetAllAsync_CalculatesWeightInPortfolioCorrectly()
    {
        var rows = new[] { HeaderRow,
            MakeRow(1, "Asset A", "Stocks", "PEA", "PEA TR", "ETF_Stocks", "", 4, "5000", "0", "0", "6000"),
            MakeRow(2, "Asset B", "Cash",   "Booklet", "Livret A", "Savings", "", 0, "4000", "0", "0", "4000")
        };
        var service = CreateService(rows);

        var result = await service.GetAllAsync();

        Assert.Equal(60m, result[0].WeightInPortfolio); // 6000 / 10000 * 100
        Assert.Equal(40m, result[1].WeightInPortfolio); // 4000 / 10000 * 100
    }

    [Fact]
    public async Task GetAllAsync_CalculatesUnrealizedGainWhenDataComplete()
    {
        var rows = new[] { HeaderRow,
            MakeRow(1, "ETF", "Stocks", "PEA", "PEA TR", "ETF_Stocks", "", 4, "5000", "500", "200", "6000")
        };
        var service = CreateService(rows);

        var result = await service.GetAllAsync();

        // unrealizedGain = currentTotal - (totalPurchases - totalSales) = 6000 - (5000 - 500) = 1500
        Assert.Equal(1500m, result[0].UnrealizedGain);
    }

    [Fact]
    public async Task GetDistributionByDimensionAsync_GroupsByAssetClass()
    {
        var assetRows = new[] { HeaderRow,
            MakeRow(1, "MSCI World", "Stocks", "PEA",     "PEA TR",  "ETF_Stocks", "", 4, "5000", "0", "0", "6000"),
            MakeRow(2, "S&P 500",   "Stocks", "CTO",     "CTO TR",  "ETF_Stocks", "", 4, "3000", "0", "0", "4000"),
            MakeRow(3, "Livret A",  "Cash",   "Booklet", "Livret A","Savings",    "", 0, "2000", "0", "0", "2000")
        };
        var refRows = new[] { RefHeader, MakeRefRow(0, "Stocks"), MakeRefRow(2, "Cash") };
        var service = CreateService(assetRows, new() { ["AssetClass"] = refRows });

        var result = await service.GetDistributionByDimensionAsync("assetClass");

        Assert.Equal(2, result.Count);
        var stocks = result.First(d => d.Name == "Stocks");
        var cash   = result.First(d => d.Name == "Cash");
        Assert.Equal(10000m, stocks.CurrentTotal);
        Assert.Equal(2000m,  cash.CurrentTotal);
        Assert.Equal(83.33m, stocks.WeightInPortfolio);
        Assert.Equal(0,      stocks.Id);
        Assert.Equal(2,      cash.Id);
    }

    [Fact]
    public async Task GetDistributionByDimensionAsync_WhenReferenceTabMissingEntry_IdIsNull()
    {
        var assetRows = new[] { HeaderRow,
            MakeRow(1, "MSCI World", "Stocks", "PEA", "PEA TR", "ETF_Stocks", "", 4, "5000", "0", "0", "6000")
        };
        var service = CreateService(assetRows);

        var result = await service.GetDistributionByDimensionAsync("assetClass");

        Assert.Single(result);
        Assert.Null(result[0].Id);
    }

    [Fact]
    public async Task GetDistributionByDimensionAsync_WhenUnknownDimension_ReturnsEmptyList()
    {
        var rows = new[] { HeaderRow,
            MakeRow(1, "MSCI World", "Stocks", "PEA", "PEA TR", "ETF_Stocks", "", 4, "5000", "0", "0", "6000")
        };
        var service = CreateService(rows);

        var result = await service.GetDistributionByDimensionAsync("unknown");

        Assert.Empty(result);
    }
}

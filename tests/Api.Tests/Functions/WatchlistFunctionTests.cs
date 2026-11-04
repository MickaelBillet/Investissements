using InvestissementsDashboard.Api.Functions;
using InvestissementsDashboard.Api.Services;
using InvestissementsDashboard.Shared.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace InvestissementsDashboard.Api.Tests.Functions;

public class WatchlistFunctionTests
{
    private readonly Mock<IWatchlistService> _service = new();

    private Task<IActionResult> InvokeAsync() =>
        new WatchlistFunction(_service.Object, NullLogger<WatchlistFunction>.Instance).GetWatchlist(new DefaultHttpContext().Request, CancellationToken.None);

    [Fact]
    public async Task GetWatchlist_ServiceAnswers_ReturnsOkWithItems()
    {
        IReadOnlyList<WatchlistItemDto> items = [new("ASML", "NL0010273215")];
        _service.Setup(s => s.GetAllAsync(It.IsAny<CancellationToken>())).ReturnsAsync(items);

        var ok = Assert.IsType<OkObjectResult>(await InvokeAsync());

        Assert.Same(items, ok.Value);
    }

    [Fact]
    public async Task GetWatchlist_WhenServiceThrows_ReturnsInternalServerError()
    {
        _service.Setup(s => s.GetAllAsync(It.IsAny<CancellationToken>())).ThrowsAsync(new InvalidOperationException("boom"));

        var status = Assert.IsType<StatusCodeResult>(await InvokeAsync());

        Assert.Equal(StatusCodes.Status500InternalServerError, status.StatusCode);
    }
}

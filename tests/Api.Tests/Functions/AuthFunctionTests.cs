using InvestissementsDashboard.Api.Functions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;

namespace InvestissementsDashboard.Api.Tests.Functions;

public class AuthFunctionTests
{
    [Fact]
    public void Verify_WhenReached_ReturnsOk()
    {
        // DashboardAuthMiddleware rejects invalid/missing passwords before this Function runs —
        // reaching this method means the password was valid.
        var result = new AuthFunction().Verify(new Mock<HttpRequest>().Object);

        Assert.IsType<OkResult>(result);
    }
}

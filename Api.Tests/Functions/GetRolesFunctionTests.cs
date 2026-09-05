using System.Text;
using System.Text.Json;
using InvestissementsDashboard.Api.Functions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace InvestissementsDashboard.Api.Tests.Functions;

public class GetRolesFunctionTests
{
    private static GetRolesFunction CreateFunction(string? ownerIdentity)
    {
        var mockConfig = new Mock<IConfiguration>();
        mockConfig.Setup(c => c["OWNER_IDENTITY"]).Returns(ownerIdentity);
        return new(mockConfig.Object, NullLogger<GetRolesFunction>.Instance);
    }

    private static HttpRequest MockRequest(string body)
    {
        var mockRequest = new Mock<HttpRequest>();
        mockRequest.Setup(r => r.Body).Returns(new MemoryStream(Encoding.UTF8.GetBytes(body)));
        return mockRequest.Object;
    }

    private static string[] ExtractRoles(IActionResult result)
    {
        var ok = Assert.IsType<OkObjectResult>(result);
        var json = JsonSerializer.Serialize(ok.Value);
        using var doc = JsonDocument.Parse(json);
        return doc.RootElement.GetProperty("roles").EnumerateArray().Select(e => e.GetString()!).ToArray();
    }

    [Fact]
    public async Task GetRoles_UserDetailsMatchesOwnerIdentity_ReturnsOwnerRole()
    {
        var body = """{"identityProvider":"aad","userId":"1","userDetails":"owner@example.com"}""";

        var result = await CreateFunction("owner@example.com").GetRoles(MockRequest(body), CancellationToken.None);

        Assert.Equal(["owner"], ExtractRoles(result));
    }

    [Fact]
    public async Task GetRoles_UserDetailsMatchesOwnerIdentity_CaseInsensitive_ReturnsOwnerRole()
    {
        var body = """{"identityProvider":"aad","userId":"1","userDetails":"Owner@Example.com"}""";

        var result = await CreateFunction("owner@example.com").GetRoles(MockRequest(body), CancellationToken.None);

        Assert.Equal(["owner"], ExtractRoles(result));
    }

    [Fact]
    public async Task GetRoles_UserDetailsDoesNotMatchOwnerIdentity_ReturnsNoRoles()
    {
        var body = """{"identityProvider":"aad","userId":"2","userDetails":"someone-else@example.com"}""";

        var result = await CreateFunction("owner@example.com").GetRoles(MockRequest(body), CancellationToken.None);

        Assert.Empty(ExtractRoles(result));
    }

    [Fact]
    public async Task GetRoles_OwnerIdentityNotConfigured_ReturnsNoRoles()
    {
        var body = """{"identityProvider":"aad","userId":"1","userDetails":"owner@example.com"}""";

        var result = await CreateFunction(null).GetRoles(MockRequest(body), CancellationToken.None);

        Assert.Empty(ExtractRoles(result));
    }

    [Fact]
    public async Task GetRoles_InvalidJsonBody_ReturnsNoRoles()
    {
        var result = await CreateFunction("owner@example.com").GetRoles(MockRequest("not-json"), CancellationToken.None);

        Assert.Empty(ExtractRoles(result));
    }
}

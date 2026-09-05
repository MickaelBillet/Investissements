using System.Net;
using System.Text.Json;
using InvestissementsDashboard.Client.Services;
using Microsoft.JSInterop;
using Moq;
using Xunit;

namespace InvestissementsDashboard.Client.Tests.Services;

public class SessionServiceTests
{
    private const string StorageKey = "investissements.dashboardSession";

    private sealed class FakeHandler(Func<HttpRequestMessage, HttpStatusCode> respond) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
            Task.FromResult(new HttpResponseMessage(respond(request)));
    }

    private static SessionService CreateService(Func<HttpRequestMessage, HttpStatusCode> respond, Mock<IJSRuntime>? jsRuntime = null)
    {
        var client = new HttpClient(new FakeHandler(respond)) { BaseAddress = new Uri("https://example.test/") };
        return new SessionService(client, (jsRuntime ?? new Mock<IJSRuntime>()).Object);
    }

    private static string StoredSessionJson(string password, DateTimeOffset expiresAt) =>
        JsonSerializer.Serialize(new { Password = password, ExpiresAt = expiresAt });

    [Fact]
    public async Task LoginAsync_WhenPasswordIsValid_SetsAuthenticatedAndStoresSession()
    {
        var jsRuntime = new Mock<IJSRuntime>();
        var service = CreateService(_ => HttpStatusCode.OK, jsRuntime);

        var result = await service.LoginAsync("correct");

        Assert.True(result);
        Assert.True(service.IsAuthenticated);
        Assert.False(service.IsSessionExpired);
        Assert.Equal("correct", service.Password);
        jsRuntime.Verify(js => js.InvokeAsync<object>(
            "localStorage.setItem", It.Is<object[]>(a => (string)a[0]! == StorageKey && ((string)a[1]!).Contains("correct"))),
            Times.Once);
    }

    [Fact]
    public async Task LoginAsync_WhenPasswordIsInvalid_DoesNotAuthenticate()
    {
        var service = CreateService(_ => HttpStatusCode.Unauthorized);

        var result = await service.LoginAsync("wrong");

        Assert.False(result);
        Assert.False(service.IsAuthenticated);
        Assert.Null(service.Password);
    }

    [Fact]
    public async Task InitializeAsync_WhenNoStoredSession_IsNotAuthenticated()
    {
        var jsRuntime = new Mock<IJSRuntime>();
        jsRuntime.Setup(js => js.InvokeAsync<string?>("localStorage.getItem", It.IsAny<object[]>()))
            .ReturnsAsync((string?)null);
        var service = CreateService(_ => HttpStatusCode.OK, jsRuntime);

        await service.InitializeAsync();

        Assert.False(service.IsAuthenticated);
    }

    [Fact]
    public async Task InitializeAsync_WhenStoredSessionIsStillValid_IsAuthenticated()
    {
        var jsRuntime = new Mock<IJSRuntime>();
        jsRuntime.Setup(js => js.InvokeAsync<string?>("localStorage.getItem", It.IsAny<object[]>()))
            .ReturnsAsync(StoredSessionJson("stored-password", DateTimeOffset.UtcNow.AddMinutes(30)));
        var service = CreateService(_ => HttpStatusCode.OK, jsRuntime);

        await service.InitializeAsync();

        Assert.True(service.IsAuthenticated);
        Assert.Equal("stored-password", service.Password);
    }

    [Fact]
    public async Task InitializeAsync_WhenStoredSessionIsExpired_IsNotAuthenticated()
    {
        var jsRuntime = new Mock<IJSRuntime>();
        jsRuntime.Setup(js => js.InvokeAsync<string?>("localStorage.getItem", It.IsAny<object[]>()))
            .ReturnsAsync(StoredSessionJson("stored-password", DateTimeOffset.UtcNow.AddMinutes(-1)));
        var service = CreateService(_ => HttpStatusCode.OK, jsRuntime);

        await service.InitializeAsync();

        Assert.False(service.IsAuthenticated);
        Assert.Null(service.Password);
    }

    [Fact]
    public async Task LogoutAsync_ClearsAuthenticationAndStoredSession()
    {
        var jsRuntime = new Mock<IJSRuntime>();
        var service = CreateService(_ => HttpStatusCode.OK, jsRuntime);
        await service.LoginAsync("correct");

        await service.LogoutAsync();

        Assert.False(service.IsAuthenticated);
        Assert.Null(service.Password);
        jsRuntime.Verify(js => js.InvokeAsync<object>(
            "localStorage.removeItem", It.Is<object[]>(a => (string)a[0]! == StorageKey)),
            Times.Once);
    }

    [Fact]
    public async Task ExtendSessionAsync_WhenAuthenticated_PersistsNewExpiry()
    {
        var jsRuntime = new Mock<IJSRuntime>();
        var service = CreateService(_ => HttpStatusCode.OK, jsRuntime);
        await service.LoginAsync("correct");

        await service.ExtendSessionAsync();

        Assert.False(service.IsSessionExpired);
        jsRuntime.Verify(js => js.InvokeAsync<object>(
            "localStorage.setItem", It.Is<object[]>(a => (string)a[0]! == StorageKey)),
            Times.Exactly(2));
    }
}

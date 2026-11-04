using System.Net;
using System.Text.Json;
using InvestissementsDashboard.Client.Services;
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

    private static SessionService CreateService(Func<HttpRequestMessage, HttpStatusCode> respond, Mock<IKeyValueStore>? store = null)
    {
        var client = new HttpClient(new FakeHandler(respond)) { BaseAddress = new Uri("https://example.test/") };
        return new SessionService(client, (store ?? new Mock<IKeyValueStore>()).Object);
    }

    private static string StoredSessionJson(string password, DateTimeOffset expiresAt) =>
        JsonSerializer.Serialize(new { Password = password, ExpiresAt = expiresAt });

    [Fact]
    public async Task LoginAsync_WhenPasswordIsValid_SetsAuthenticatedAndStoresSession()
    {
        var store = new Mock<IKeyValueStore>();
        var service = CreateService(_ => HttpStatusCode.OK, store);

        var result = await service.LoginAsync("correct");

        Assert.True(result);
        Assert.True(service.IsAuthenticated);
        Assert.False(service.IsSessionExpired);
        Assert.Equal("correct", service.Password);
        store.Verify(s => s.SetAsync(StorageKey, It.Is<string>(v => v.Contains("correct"))), Times.Once);
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
        var store = new Mock<IKeyValueStore>();
        store.Setup(s => s.GetAsync(StorageKey))
            .ReturnsAsync((string?)null);
        var service = CreateService(_ => HttpStatusCode.OK, store);

        await service.InitializeAsync();

        Assert.False(service.IsAuthenticated);
    }

    [Fact]
    public async Task InitializeAsync_WhenStoredSessionIsStillValid_IsAuthenticated()
    {
        var store = new Mock<IKeyValueStore>();
        store.Setup(s => s.GetAsync(StorageKey))
            .ReturnsAsync(StoredSessionJson("stored-password", DateTimeOffset.UtcNow.AddMinutes(30)));
        var service = CreateService(_ => HttpStatusCode.OK, store);

        await service.InitializeAsync();

        Assert.True(service.IsAuthenticated);
        Assert.Equal("stored-password", service.Password);
    }

    [Fact]
    public async Task InitializeAsync_WhenStoredSessionIsExpired_IsNotAuthenticated()
    {
        var store = new Mock<IKeyValueStore>();
        store.Setup(s => s.GetAsync(StorageKey))
            .ReturnsAsync(StoredSessionJson("stored-password", DateTimeOffset.UtcNow.AddMinutes(-1)));
        var service = CreateService(_ => HttpStatusCode.OK, store);

        await service.InitializeAsync();

        Assert.False(service.IsAuthenticated);
        Assert.Null(service.Password);
    }

    [Fact]
    public async Task LogoutAsync_ClearsAuthenticationAndStoredSession()
    {
        var store = new Mock<IKeyValueStore>();
        var service = CreateService(_ => HttpStatusCode.OK, store);
        await service.LoginAsync("correct");

        await service.LogoutAsync();

        Assert.False(service.IsAuthenticated);
        Assert.Null(service.Password);
        store.Verify(s => s.RemoveAsync(StorageKey), Times.Once);
    }

    [Fact]
    public async Task ExtendSessionAsync_WhenAuthenticated_PersistsNewExpiry()
    {
        var store = new Mock<IKeyValueStore>();
        var service = CreateService(_ => HttpStatusCode.OK, store);
        await service.LoginAsync("correct");

        await service.ExtendSessionAsync();

        Assert.False(service.IsSessionExpired);
        store.Verify(s => s.SetAsync(StorageKey, It.IsAny<string>()), Times.Exactly(2));
    }
}

using System.Net;
using System.Text;
using InvestissementsDashboard.Api.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Moq.Protected;
using Xunit;

namespace InvestissementsDashboard.Api.Tests.Services;

public class SyncServiceTests
{
    private static SyncService CreateService(HttpResponseMessage response, string? syncUrl = "https://script.google.com/exec", string? syncKey = "secret")
    {
        var handlerMock = new Mock<HttpMessageHandler>();
        handlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(response);

        var httpClient = new HttpClient(handlerMock.Object);

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["APPS_SCRIPT_SYNC_URL"] = syncUrl,
                ["APPS_SCRIPT_SYNC_KEY"] = syncKey
            })
            .Build();

        return new SyncService(httpClient, configuration, NullLogger<SyncService>.Instance);
    }

    private static HttpResponseMessage JsonResponse(HttpStatusCode statusCode, string json)
        => new(statusCode) { Content = new StringContent(json, Encoding.UTF8, "application/json") };

    [Fact]
    public async Task TriggerAsync_WhenAppsScriptSucceeds_ReturnsSuccessWithAddedCount()
    {
        var service = CreateService(JsonResponse(HttpStatusCode.OK, """{"success":true,"addedCount":2}"""));

        var result = await service.TriggerAsync();

        Assert.True(result.Success);
        Assert.Equal(2, result.AddedCount);
        Assert.Null(result.ErrorMessage);
    }

    [Fact]
    public async Task TriggerAsync_WhenAppsScriptReturnsFailure_ReturnsErrorMessage()
    {
        var service = CreateService(JsonResponse(HttpStatusCode.OK, """{"success":false,"error":"Unauthorized"}"""));

        var result = await service.TriggerAsync();

        Assert.False(result.Success);
        Assert.Equal(0, result.AddedCount);
        Assert.Equal("Unauthorized", result.ErrorMessage);
    }

    [Fact]
    public async Task TriggerAsync_WhenConfigurationMissing_ReturnsErrorWithoutCallingHttp()
    {
        var service = CreateService(JsonResponse(HttpStatusCode.OK, "{}"), syncUrl: null, syncKey: null);

        var result = await service.TriggerAsync();

        Assert.False(result.Success);
        Assert.Equal(0, result.AddedCount);
        Assert.NotNull(result.ErrorMessage);
    }

    [Fact]
    public async Task TriggerAsync_WhenHttpCallThrows_ReturnsErrorMessage()
    {
        var handlerMock = new Mock<HttpMessageHandler>();
        handlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
            .ThrowsAsync(new HttpRequestException("network error"));

        var httpClient = new HttpClient(handlerMock.Object);
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["APPS_SCRIPT_SYNC_URL"] = "https://script.google.com/exec",
                ["APPS_SCRIPT_SYNC_KEY"] = "secret"
            })
            .Build();

        var service = new SyncService(httpClient, configuration, NullLogger<SyncService>.Instance);

        var result = await service.TriggerAsync();

        Assert.False(result.Success);
        Assert.Equal(0, result.AddedCount);
        Assert.NotNull(result.ErrorMessage);
    }
}

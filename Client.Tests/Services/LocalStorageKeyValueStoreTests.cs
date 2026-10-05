using InvestissementsDashboard.Client.Services;
using Microsoft.JSInterop;
using Moq;
using Xunit;

namespace InvestissementsDashboard.Client.Tests.Services;

public class LocalStorageKeyValueStoreTests
{
    [Fact]
    public async Task GetAsync_CallsLocalStorageGetItem()
    {
        var js = new Mock<IJSRuntime>();
        js.Setup(j => j.InvokeAsync<string?>("localStorage.getItem", It.IsAny<object[]>())).ReturnsAsync("value");

        var result = await new LocalStorageKeyValueStore(js.Object).GetAsync("k");

        Assert.Equal("value", result);
        js.Verify(j => j.InvokeAsync<string?>("localStorage.getItem", It.Is<object[]>(a => (string)a[0]! == "k")), Times.Once);
    }

    [Fact]
    public async Task SetAsync_CallsLocalStorageSetItem()
    {
        var js = new Mock<IJSRuntime>();

        await new LocalStorageKeyValueStore(js.Object).SetAsync("k", "v");

        js.Verify(j => j.InvokeAsync<object>("localStorage.setItem",
            It.Is<object[]>(a => (string)a[0]! == "k" && (string)a[1]! == "v")), Times.Once);
    }

    [Fact]
    public async Task RemoveAsync_CallsLocalStorageRemoveItem()
    {
        var js = new Mock<IJSRuntime>();

        await new LocalStorageKeyValueStore(js.Object).RemoveAsync("k");

        js.Verify(j => j.InvokeAsync<object>("localStorage.removeItem",
            It.Is<object[]>(a => (string)a[0]! == "k")), Times.Once);
    }
}

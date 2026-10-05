using InvestissementsDashboard.Maui.Services;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace InvestissementsDashboard.Maui.Tests;

public class SecureStorageKeyValueStoreTests
{
    private static SecureStorageKeyValueStore CreateStore(Mock<ISecureStorage> secureStorage) =>
        new(secureStorage.Object, NullLogger<SecureStorageKeyValueStore>.Instance);

    [Fact]
    public async Task GetAsync_WhenValueExists_ReturnsIt()
    {
        var secureStorage = new Mock<ISecureStorage>();
        secureStorage.Setup(s => s.GetAsync("k")).ReturnsAsync("v");

        var result = await CreateStore(secureStorage).GetAsync("k");

        Assert.Equal("v", result);
    }

    [Fact]
    public async Task GetAsync_WhenStorageThrows_ReturnsNullAndRemovesEntry()
    {
        var secureStorage = new Mock<ISecureStorage>();
        secureStorage.Setup(s => s.GetAsync("k")).ThrowsAsync(new InvalidOperationException("cannot decrypt"));

        var result = await CreateStore(secureStorage).GetAsync("k");

        Assert.Null(result);
        secureStorage.Verify(s => s.Remove("k"), Times.Once);
    }

    [Fact]
    public async Task SetAsync_DelegatesToSecureStorage()
    {
        var secureStorage = new Mock<ISecureStorage>();

        await CreateStore(secureStorage).SetAsync("k", "v");

        secureStorage.Verify(s => s.SetAsync("k", "v"), Times.Once);
    }

    [Fact]
    public async Task RemoveAsync_DelegatesToSecureStorage()
    {
        var secureStorage = new Mock<ISecureStorage>();

        await CreateStore(secureStorage).RemoveAsync("k");

        secureStorage.Verify(s => s.Remove("k"), Times.Once);
    }
}

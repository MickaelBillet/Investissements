using InvestissementsDashboard.Client.Services;
using Moq;
using Xunit;

namespace InvestissementsDashboard.Client.Tests.Services;

public class PrivacyModeServiceTests
{
    private const string StorageKey = "investissements.hideAmounts";

    [Theory]
    [InlineData("true", true)]
    [InlineData("false", false)]
    [InlineData(null, false)]
    public async Task InitializeAsync_WithStoredValue_SetsIsHiddenAccordingly(string? stored, bool expected)
    {
        var store = new Mock<IKeyValueStore>();
        store.Setup(s => s.GetAsync(StorageKey)).ReturnsAsync(stored);
        var service = new PrivacyModeService(store.Object);

        await service.InitializeAsync();

        Assert.Equal(expected, service.IsHidden);
    }

    [Fact]
    public async Task ToggleAsync_WhenVisible_HidesAndPersistsTrue()
    {
        var store = new Mock<IKeyValueStore>();
        var service = new PrivacyModeService(store.Object);
        var notified = false;
        service.OnChange += () => notified = true;

        await service.ToggleAsync();

        Assert.True(service.IsHidden);
        Assert.True(notified);
        store.Verify(s => s.SetAsync(StorageKey, "true"), Times.Once);
    }
}

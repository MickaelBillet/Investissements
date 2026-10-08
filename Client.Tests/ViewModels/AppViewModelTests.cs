using InvestissementsDashboard.Client.Services;
using InvestissementsDashboard.Client.ViewModels;
using Moq;

namespace InvestissementsDashboard.Client.Tests.ViewModels;

public class AppViewModelTests
{
    [Fact]
    public void IsAuthenticated_ReflectsSessionState()
    {
        var mock = new Mock<ISessionService>();
        mock.SetupGet(s => s.IsAuthenticated).Returns(true);

        Assert.True(new AppViewModel(mock.Object).IsAuthenticated);
    }

    [Fact]
    public async Task InitializeAsync_InitializesSession()
    {
        var mock = new Mock<ISessionService>();

        await new AppViewModel(mock.Object).InitializeAsync();

        mock.Verify(s => s.InitializeAsync(), Times.Once);
    }

    [Fact]
    public void OnChange_WhenSessionRaisesEvent_NotifiesSubscribers()
    {
        var mock = new Mock<ISessionService>();
        var vm = new AppViewModel(mock.Object);
        var raised = 0;
        vm.OnChange += () => raised++;

        mock.Raise(s => s.OnChange += null);

        Assert.Equal(1, raised);
    }

    [Fact]
    public void OnChange_AfterUnsubscribe_DoesNotNotify()
    {
        var mock = new Mock<ISessionService>();
        var vm = new AppViewModel(mock.Object);
        var raised = 0;
        void Handler() => raised++;
        vm.OnChange += Handler;
        vm.OnChange -= Handler;

        mock.Raise(s => s.OnChange += null);

        Assert.Equal(0, raised);
    }
}

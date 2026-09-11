using InvestissementsDashboard.Client.Services;
using InvestissementsDashboard.Client.ViewModels;
using Moq;

namespace InvestissementsDashboard.Client.Tests.ViewModels;

public class LoginGateViewModelTests
{
    [Fact]
    public async Task SubmitAsync_WithValidPassword_SetsShowErrorFalse()
    {
        var mock = new Mock<ISessionService>();
        mock.Setup(s => s.LoginAsync("correct")).ReturnsAsync(true);
        var vm = new LoginGateViewModel(mock.Object) { Password = "correct" };

        await vm.SubmitAsync();

        Assert.False(vm.ShowError);
        Assert.False(vm.IsSubmitting);
    }

    [Fact]
    public async Task SubmitAsync_WithInvalidPassword_SetsShowErrorTrue()
    {
        var mock = new Mock<ISessionService>();
        mock.Setup(s => s.LoginAsync("wrong")).ReturnsAsync(false);
        var vm = new LoginGateViewModel(mock.Object) { Password = "wrong" };

        await vm.SubmitAsync();

        Assert.True(vm.ShowError);
        Assert.False(vm.IsSubmitting);
    }

    [Fact]
    public async Task SubmitAsync_PassesPasswordToSessionService()
    {
        var mock = new Mock<ISessionService>();
        mock.Setup(s => s.LoginAsync(It.IsAny<string>())).ReturnsAsync(true);
        var vm = new LoginGateViewModel(mock.Object) { Password = "hunter2" };

        await vm.SubmitAsync();

        mock.Verify(s => s.LoginAsync("hunter2"), Times.Once);
    }
}

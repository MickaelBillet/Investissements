using InvestissementsDashboard.Client.Model;
using InvestissementsDashboard.Client.Services;
using InvestissementsDashboard.Client.ViewModels;
using Moq;

namespace InvestissementsDashboard.Client.Tests.ViewModels;

public class LoginGateViewModelTests
{
    [Fact]
    public async Task SubmitAsync_WithValidPassword_HasNoError()
    {
        var mock = new Mock<ISessionService>();
        mock.Setup(s => s.LoginAsync("correct")).ReturnsAsync(LoginResult.Success);
        var vm = new LoginGateViewModel(mock.Object) { Password = "correct" };

        await vm.SubmitAsync();

        Assert.Null(vm.ErrorKey);
        Assert.False(vm.IsSubmitting);
    }

    [Fact]
    public async Task SubmitAsync_WithInvalidPassword_ShowsInvalidPassword()
    {
        var mock = new Mock<ISessionService>();
        mock.Setup(s => s.LoginAsync("wrong")).ReturnsAsync(LoginResult.InvalidPassword);
        var vm = new LoginGateViewModel(mock.Object) { Password = "wrong" };

        await vm.SubmitAsync();

        Assert.Equal("Login_InvalidPassword", vm.ErrorKey);
        Assert.False(vm.IsSubmitting);
    }

    [Fact]
    public async Task SubmitAsync_ServiceUnavailable_ShowsServiceUnavailable()
    {
        var mock = new Mock<ISessionService>();
        mock.Setup(s => s.LoginAsync(It.IsAny<string>())).ReturnsAsync(LoginResult.ServiceUnavailable);
        var vm = new LoginGateViewModel(mock.Object) { Password = "correct" };

        await vm.SubmitAsync();

        Assert.Equal("Login_ServiceUnavailable", vm.ErrorKey);
        Assert.False(vm.IsSubmitting);
    }

    [Fact]
    public async Task SubmitAsync_WhileRetrying_ClearsThePreviousError()
    {
        var retry = new TaskCompletionSource<LoginResult>();
        var mock = new Mock<ISessionService>();
        mock.SetupSequence(s => s.LoginAsync(It.IsAny<string>()))
            .ReturnsAsync(LoginResult.InvalidPassword)
            .Returns(retry.Task);
        var vm = new LoginGateViewModel(mock.Object) { Password = "wrong" };
        await vm.SubmitAsync();

        var submit = vm.SubmitAsync();

        Assert.Null(vm.ErrorKey);
        retry.SetResult(LoginResult.Success);
        await submit;
    }

    [Fact]
    public async Task SubmitAsync_PassesPasswordToSessionService()
    {
        var mock = new Mock<ISessionService>();
        mock.Setup(s => s.LoginAsync(It.IsAny<string>())).ReturnsAsync(LoginResult.Success);
        var vm = new LoginGateViewModel(mock.Object) { Password = "hunter2" };

        await vm.SubmitAsync();

        mock.Verify(s => s.LoginAsync("hunter2"), Times.Once);
    }
}

using Bunit;
using InvestissementsDashboard.Client.Services;
using InvestissementsDashboard.Client.Shared;
using InvestissementsDashboard.Client.Tests.Helpers;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using MudBlazor.Services;

namespace InvestissementsDashboard.Client.Tests.Components;

public class LoginGateTests : BunitContext
{
    public LoginGateTests()
    {
        Services.AddMudServices(opt => opt.PopoverOptions.CheckForPopoverProvider = false);
        Services.AddLocalizationMock();
        JSInterop.Mode = JSRuntimeMode.Loose;
    }

    [Fact]
    public void LoginGate_WhenPasswordIsInvalid_ShowsErrorMessage()
    {
        var session = new Mock<ISessionService>();
        session.Setup(s => s.LoginAsync(It.IsAny<string>())).ReturnsAsync(false);
        Services.AddSingleton(session.Object);

        var cut = Render<LoginGate>();
        cut.Find("input").Change("wrong-password");
        cut.Find("button").Click();

        Assert.Contains("Mot de passe incorrect", cut.Markup);
    }

    [Fact]
    public void LoginGate_WhenPasswordIsValid_InvokesLoginAsync()
    {
        var session = new Mock<ISessionService>();
        session.Setup(s => s.LoginAsync("correct")).ReturnsAsync(true);
        Services.AddSingleton(session.Object);

        var cut = Render<LoginGate>();
        cut.Find("input").Change("correct");
        cut.Find("button").Click();

        session.Verify(s => s.LoginAsync("correct"), Times.Once);
        Assert.DoesNotContain("Mot de passe incorrect", cut.Markup);
    }
}

using Bunit;
using InvestissementsDashboard.Client.Services;
using InvestissementsDashboard.Client.Shared.Dialogs;
using InvestissementsDashboard.Client.Tests.Helpers;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using MudBlazor;
using MudBlazor.Services;

namespace InvestissementsDashboard.Client.Tests.Components;

public class AgentResultDialogTests : BunitContext, IAsyncLifetime
{
    // MudBlazor's dialog services only implement IAsyncDisposable, which the synchronous
    // BunitContext.Dispose() cannot release: dispose asynchronously instead.
    public Task InitializeAsync() => Task.CompletedTask;

    Task IAsyncLifetime.DisposeAsync() => base.DisposeAsync().AsTask();

    private readonly Mock<IAgentRunner> _runner = new();

    public AgentResultDialogTests()
    {
        Services.AddMudServices(opt => opt.PopoverOptions.CheckForPopoverProvider = false);
        Services.AddLocalizationMock();
        Services.AddAgentViewModel(_runner.Object);
        JSInterop.Mode = JSRuntimeMode.Loose;
    }

    private IRenderedComponent<MudDialogProvider> Open()
    {
        var provider = Render<MudDialogProvider>();
        var dialogs = Services.GetRequiredService<IDialogService>();
        provider.InvokeAsync(() => dialogs.ShowAsync<AgentResultDialog>(
            "Air Liquide",
            new DialogParameters<AgentResultDialog> { { x => x.Agent, AgentChoice.Stock }, { x => x.AssetName, "Air Liquide" } }));
        return provider;
    }

    [Fact]
    public void AgentResultDialog_WhileAgentRuns_ShowsProgress()
    {
        var pending = new TaskCompletionSource<string>();
        _runner.Setup(r => r.RunAsync(AgentChoice.Stock, "Air Liquide", It.IsAny<CancellationToken>()))
               .Returns(pending.Task);

        var provider = Open();

        Assert.Contains("L'agent travaille", provider.Markup);
    }

    [Fact]
    public void AgentResultDialog_WhenAgentReplies_ShowsResponse()
    {
        _runner.Setup(r => r.RunAsync(AgentChoice.Stock, "Air Liquide", It.IsAny<CancellationToken>()))
               .ReturnsAsync("Analyse terminée");

        var provider = Open();

        provider.WaitForAssertion(() => Assert.Contains("Analyse terminée", provider.Markup));
    }

    [Fact]
    public void AgentResultDialog_WhenAgentThrows_ShowsError()
    {
        _runner.Setup(r => r.RunAsync(It.IsAny<AgentChoice>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
               .ThrowsAsync(new InvalidOperationException("boom"));

        var provider = Open();

        provider.WaitForAssertion(() =>
        {
            Assert.Contains("L'agent a échoué", provider.Markup);
            Assert.Contains("boom", provider.Markup);
        });
    }
}

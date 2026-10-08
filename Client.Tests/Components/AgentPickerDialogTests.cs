using Bunit;
using InvestissementsDashboard.Client.Services;
using InvestissementsDashboard.Client.Shared.Dialogs;
using InvestissementsDashboard.Client.Tests.Helpers;
using Microsoft.Extensions.DependencyInjection;
using MudBlazor;
using MudBlazor.Services;

namespace InvestissementsDashboard.Client.Tests.Components;

public class AgentPickerDialogTests : BunitContext, IAsyncLifetime
{
    public AgentPickerDialogTests()
    {
        Services.AddMudServices(opt => opt.PopoverOptions.CheckForPopoverProvider = false);
        Services.AddLocalizationMock();
        JSInterop.Mode = JSRuntimeMode.Loose;
    }

    // MudBlazor's dialog services only implement IAsyncDisposable, which the synchronous
    // BunitContext.Dispose() cannot release: dispose asynchronously instead.
    public Task InitializeAsync() => Task.CompletedTask;

    Task IAsyncLifetime.DisposeAsync() => base.DisposeAsync().AsTask();

    private async Task<(IRenderedComponent<MudDialogProvider> Provider, IDialogReference Reference)> OpenAsync()
    {
        var provider = Render<MudDialogProvider>();
        var dialogs = Services.GetRequiredService<IDialogService>();
        var reference = await provider.InvokeAsync(() => dialogs.ShowAsync<AgentPickerDialog>(
            "title", new DialogParameters<AgentPickerDialog> { { x => x.AssetName, "Air Liquide" } }));
        return (provider, reference);
    }

    [Fact]
    public async Task AgentPickerDialog_WhenStockClicked_ReturnsStockChoice()
    {
        var (provider, reference) = await OpenAsync();

        provider.Find("#agent-stock").Click();
        var result = await reference.Result;

        Assert.False(result!.Canceled);
        Assert.Equal(AgentChoice.Stock, result.Data);
    }

    [Fact]
    public async Task AgentPickerDialog_WhenNewsClicked_ReturnsNewsChoice()
    {
        var (provider, reference) = await OpenAsync();

        provider.Find("#agent-news").Click();
        var result = await reference.Result;

        Assert.Equal(AgentChoice.News, result!.Data);
    }

    [Fact]
    public async Task AgentPickerDialog_WhenCancelled_ReturnsCanceledResult()
    {
        var (provider, reference) = await OpenAsync();

        provider.FindAll("button").Single(b => b.TextContent.Contains("Annuler")).Click();
        var result = await reference.Result;

        Assert.True(result!.Canceled);
    }
}

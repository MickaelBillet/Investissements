using InvestissementsDashboard.Client.Extensions;
using InvestissementsDashboard.Client.Services;
using InvestissementsDashboard.Client.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Xunit;

namespace InvestissementsDashboard.Client.Tests.Extensions;

public class ServiceCollectionExtensionsTests
{
    private static ServiceProvider BuildProvider(Uri apiBase)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton(new Mock<IKeyValueStore>().Object);
        services.AddInvestissementsClient(apiBase);
        return services.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true, ValidateOnBuild = false });
    }

    [Fact]
    public void AddInvestissementsClient_ResolvesSingletonServices()
    {
        using var provider = BuildProvider(new Uri("https://example.test/"));

        Assert.NotNull(provider.GetRequiredService<ISessionService>());
        Assert.NotNull(provider.GetRequiredService<IPrivacyModeService>());
        Assert.NotNull(provider.GetRequiredService<ILocalizationService>());
        Assert.NotNull(provider.GetRequiredService<IPortfolioService>());
        Assert.NotNull(provider.GetRequiredService<ISyncService>());
    }

    [Fact]
    public void AddInvestissementsClient_ResolvesViewModelsInScope()
    {
        using var provider = BuildProvider(new Uri("https://example.test/"));
        using var scope = provider.CreateScope();

        Assert.NotNull(scope.ServiceProvider.GetRequiredService<DashboardViewModel>());
        Assert.NotNull(scope.ServiceProvider.GetRequiredService<TrackingViewModel>());
        Assert.NotNull(scope.ServiceProvider.GetRequiredService<LoginGateViewModel>());
        Assert.NotNull(scope.ServiceProvider.GetRequiredService<AppViewModel>());
        Assert.NotNull(scope.ServiceProvider.GetRequiredService<MainViewModel>());
        Assert.NotNull(scope.ServiceProvider.GetRequiredService<AssetViewModel>());
        Assert.NotNull(scope.ServiceProvider.GetRequiredService<AgentViewModel>());
    }

    [Fact]
    public void AddInvestissementsClient_ExposesDocumentationUriRelativeToApiBase()
    {
        using var provider = BuildProvider(new Uri("https://example.test/"));

        var options = provider.GetRequiredService<ClientOptions>();

        Assert.Equal("https://example.test/docs/presentation.pdf", options.DocumentationUri.ToString());
    }
}

using System.Net;
using AgentAI;
using InvestissementsDashboard.Client.Services;
using InvestissementsDashboard.Maui.Services;
using InvestissementsDashboard.Shared.Models;
using Microsoft.Agents.AI.Workflows;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace InvestissementsDashboard.Maui.Tests;

/// <summary>
/// Tests the runner alone: the builder is mocked and returns a one-step workflow (standing for the synthesis) whose behaviour each test chooses,
/// so these tests do not depend on how the real workflow is assembled (see PortfolioReviewWorkflowBuilderTests).
/// </summary>
public class PortfolioReviewRunnerTests
{
    private static readonly DateOnly Date = new(2026, 10, 19);

    private readonly Mock<IAgentConfigSource> _config = new();
    private readonly Mock<IAgentFactory> _factory = new();
    private readonly Mock<IPortfolioReviewWorkflowBuilder> _builder = new();
    private readonly Mock<IAssetDistributionSource> _distributionSource = new();

    public PortfolioReviewRunnerTests()
    {
        _config.Setup(c => c.GetAsync(It.IsAny<CancellationToken>())).ReturnsAsync(new AgentConfigDto("https://foundry.test/", null));
    }

    private PortfolioReviewRunner CreateRunner(TimeSpan? timeout = null) => new(
        new AgentOptionsProvider(_config.Object, new Uri("https://invest.test/"), @"C:\history"),
        _ => _factory.Object,
        () => _distributionSource.Object,
        _builder.Object,
        NullLogger<PortfolioReviewRunner>.Instance,
        timeout);

    private void SetupStep(Func<AnalysisRequest, CancellationToken, ValueTask<SynthesisReport>> handle)
    {
        var step = new TestStep(handle);
        _builder
            .Setup(b => b.BuildAsync(It.IsAny<IAgentFactory>(), It.IsAny<IAssetDistributionSource>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(() => new WorkflowBuilder(step).WithOutputFrom(step).Build());
    }

    private static SynthesisReport EmptyReport() => new([], [], []);

    // Honours the token like a real model call: a delay that ignores it would keep the workflow's background run alive.
    private static async ValueTask<SynthesisReport> NeverAnswers(AnalysisRequest _, CancellationToken ct)
    {
        await Task.Delay(Timeout.Infinite, ct);
        return EmptyReport();
    }

    [Fact]
    public async Task RunAsync_WorkflowYieldsSynthesis_CompletesWithTheRequestedDate()
    {
        DateOnly? received = null;
        SetupStep((request, _) =>
        {
            received = request.Date;
            return ValueTask.FromResult(EmptyReport());
        });

        await CreateRunner().RunAsync(Date);

        Assert.Equal(Date, received);
    }

    [Fact]
    public async Task RunAsync_Always_PassesTheRunFactoryToTheBuilder()
    {
        SetupStep((_, _) => ValueTask.FromResult(EmptyReport()));

        await CreateRunner().RunAsync(Date);

        _builder.Verify(b => b.BuildAsync(_factory.Object, It.IsAny<IAssetDistributionSource>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task RunAsync_Always_PassesTheRunDistributionSourceToTheBuilder()
    {
        SetupStep((_, _) => ValueTask.FromResult(EmptyReport()));

        await CreateRunner().RunAsync(Date);

        _builder.Verify(b => b.BuildAsync(It.IsAny<IAgentFactory>(), _distributionSource.Object, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task RunAsync_StepThrows_ThrowsInvalidOperationNamingTheStep()
    {
        SetupStep((_, _) => throw new InvalidOperationException("model unavailable"));

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => CreateRunner().RunAsync(Date));

        Assert.Contains(TestStep.Id, ex.Message);
        Assert.Contains("model unavailable", ex.Message);
    }

    [Fact]
    public async Task RunAsync_McpUnauthorized_ThrowsPasswordHint()
    {
        _builder
            .Setup(b => b.BuildAsync(It.IsAny<IAgentFactory>(), It.IsAny<IAssetDistributionSource>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new HttpRequestException("401", null, HttpStatusCode.Unauthorized));

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => CreateRunner().RunAsync(Date));

        Assert.Contains("dashboard password", ex.Message);
    }

    [Fact]
    public async Task RunAsync_StepNeverAnswers_ThrowsTimeout()
    {
        SetupStep(NeverAnswers);

        await Assert.ThrowsAsync<TimeoutException>(() => CreateRunner(TimeSpan.FromMilliseconds(100)).RunAsync(Date));
    }

    [Fact]
    public async Task RunAsync_CallerCancels_ThrowsOperationCanceled()
    {
        SetupStep(NeverAnswers);
        using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(100));

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => CreateRunner().RunAsync(Date, cts.Token));
    }

    private sealed class TestStep(Func<AnalysisRequest, CancellationToken, ValueTask<SynthesisReport>> handle)
        : Executor<AnalysisRequest, SynthesisReport>(Id)
    {
        public const string Id = "test-step";

        public override ValueTask<SynthesisReport> HandleAsync(AnalysisRequest message, IWorkflowContext context, CancellationToken cancellationToken = default) =>
            handle(message, cancellationToken);
    }
}

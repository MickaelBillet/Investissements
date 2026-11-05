using System.Text.Encodings.Web;
using System.Text.Json;
using AgentAI.Core.Workflows.PortfolioReview.Events;
using AgentAI.Core.Workflows.PortfolioReview.Messages;
using InvestissementsDashboard.Shared.Models;
using Microsoft.Agents.AI;
using Microsoft.Agents.AI.Workflows;

namespace AgentAI.Core.Workflows.PortfolioReview.Executors;

/// <summary>
/// Has the Synthesis agent combine the allocation, the context and the asset distributions into the review's output.
/// </summary>
/// <remarks>
/// The fan-in barrier delivers its three inputs as three separate messages, in no guaranteed order. Each handler
/// keeps its input in the workflow state (rather than in fields, so a checkpoint would capture it) and the report
/// is written once all three are there.
/// The watchlist comes with the allocation; the scenarios and convictions come with the agent's instructions
/// (<c>InvestorProfile.md</c>).
/// </remarks>
internal sealed class SynthesisExecutor(AIAgent agent) : Executor(PortfolioReviewSteps.Synthesis)
{
    private const string AllocationKey = "allocation";
    private const string ContextKey = "context";
    private const string DistributionKey = "distribution";

    // Accents kept as is: escaping them would only grow the prompt the model has to read.
    private static readonly JsonSerializerOptions PromptJsonOptions = new(JsonSerializerDefaults.Web) { Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping };

    protected override ProtocolBuilder ConfigureProtocol(ProtocolBuilder protocolBuilder) =>
        protocolBuilder
            .ConfigureRoutes(routes => routes
                .AddHandler<AllocationResult>(this.OnAllocationAsync)
                .AddHandler<PortfolioContext>(this.OnContextAsync)
                .AddHandler<DistributionResult>(this.OnDistributionAsync))
            .YieldsOutput<SynthesisReport>();

    private async ValueTask OnAllocationAsync(AllocationResult allocation, IWorkflowContext context, CancellationToken cancellationToken)
    {
        await context.QueueStateUpdateAsync(AllocationKey, allocation, cancellationToken);
        await this.WriteWhenCompleteAsync(context, cancellationToken);
    }

    private async ValueTask OnContextAsync(PortfolioContext portfolioContext, IWorkflowContext context, CancellationToken cancellationToken)
    {
        await context.QueueStateUpdateAsync(ContextKey, portfolioContext, cancellationToken);
        await this.WriteWhenCompleteAsync(context, cancellationToken);
    }

    private async ValueTask OnDistributionAsync(DistributionResult distribution, IWorkflowContext context, CancellationToken cancellationToken)
    {
        await context.QueueStateUpdateAsync(DistributionKey, distribution, cancellationToken);
        await this.WriteWhenCompleteAsync(context, cancellationToken);
    }

    private async ValueTask WriteWhenCompleteAsync(IWorkflowContext context, CancellationToken cancellationToken)
    {
        var allocation = await context.ReadStateAsync<AllocationResult>(AllocationKey, cancellationToken);
        var portfolioContext = await context.ReadStateAsync<PortfolioContext>(ContextKey, cancellationToken);
        var distribution = await context.ReadStateAsync<DistributionResult>(DistributionKey, cancellationToken);
        if (allocation is null || portfolioContext is null || distribution is null)
        {
            return;
        }

        var prompt = BuildPrompt(allocation, portfolioContext, distribution);
        var response = await agent.RunAsync<SynthesisReport>(prompt, cancellationToken: cancellationToken);
        var report = response.Result;

        await context.AddEventAsync(new SynthesisWrittenEvent(report), cancellationToken);
        await context.YieldOutputAsync(report, cancellationToken);
    }

    // French because it is the agent's working language.
    // The date comes from the context, which ContextExecutor forces to the requested one; the allocation's is the model's.
    private static string BuildPrompt(AllocationResult allocation, PortfolioContext portfolioContext, DistributionResult distribution) =>
        $"""
        Rédige la synthèse du portefeuille au {portfolioContext.Date:yyyy-MM-dd}.

        Allocation actuelle :
        {JsonSerializer.Serialize(allocation, PromptJsonOptions)}

        Contexte du jour :
        {JsonSerializer.Serialize(portfolioContext, PromptJsonOptions)}

        Expositions calculées (actions et obligations uniquement) :
        {JsonSerializer.Serialize(distribution, PromptJsonOptions)}
        """;
}

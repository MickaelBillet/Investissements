using Microsoft.Agents.AI;
using Microsoft.Agents.AI.Workflows;

namespace AgentAI;

/// <summary>
/// Establishes the economic and geopolitical context at the requested date, through the Context agent and its web
/// search; runs in parallel with <see cref="AllocationExecutor"/> and knows nothing of the portfolio.
/// </summary>
public sealed class ContextExecutor(AIAgent agent) : Executor<AnalysisRequest, PortfolioContext>("context")
{
    public override async ValueTask<PortfolioContext> HandleAsync(AnalysisRequest message, IWorkflowContext context, CancellationToken cancellationToken = default)
    {
        // French because it is the agent's working language; what to establish lives in its instructions (Context.md).
        var prompt = $"Décris moi de façon détaillée le contexte économique et géopolitique au {message.Date:yyyy-MM-dd}.";

        var response = await agent.RunAsync<PortfolioContext>(prompt, cancellationToken: cancellationToken);

        // The date is known: it is not left to the model, which could restate it differently.
        var portfolioContext = response.Result with { Date = message.Date };

        await context.AddEventAsync(new ContextRetrievedEvent(portfolioContext), cancellationToken);
        return portfolioContext;
    }
}

using AgentAI.Core.Workflows.PortfolioReview.Events;
using AgentAI.Core.Workflows.PortfolioReview.Messages;
using Microsoft.Agents.AI;
using Microsoft.Agents.AI.Workflows;

namespace AgentAI.Core.Workflows.PortfolioReview.Executors;

/// <summary>
/// Establishes the economic and geopolitical context at the requested date, through the Context agent and its web
/// search; runs in parallel with <see cref="AllocationExecutor"/> and knows nothing of the portfolio.
/// </summary>
internal sealed class ContextExecutor(AIAgent agent) : Executor<AnalysisRequest, PortfolioContext>("context")
{
    public override async ValueTask<PortfolioContext> HandleAsync(AnalysisRequest message, IWorkflowContext context, CancellationToken cancellationToken = default)
    {
        // French because it is the agent's working language; what to establish lives in its instructions (Context.md).
        var prompt = $"Décris moi de façon détaillée le contexte économique et géopolitique au {message.Date:yyyy-MM-dd}.";

        var response = await agent.RunAsync<PortfolioContext>(prompt, cancellationToken: cancellationToken);

        // Search errors go to the model, not to the workflow: without this check the synthesis would still write a
        // report, with recommendations that no factor supports.
        if (response.Result.Factors.Count == 0)
        {
            throw new InvalidOperationException("the context came back with no factor: the web searches probably failed (see the log).");
        }

        // The date is known: it is not left to the model, which could restate it differently.
        var portfolioContext = response.Result with { Date = message.Date };

        await context.AddEventAsync(new ContextRetrievedEvent(portfolioContext), cancellationToken);
        return portfolioContext;
    }
}

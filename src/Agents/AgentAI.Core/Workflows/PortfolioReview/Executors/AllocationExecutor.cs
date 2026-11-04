using AgentAI.Core.Workflows.PortfolioReview.Events;
using AgentAI.Core.Workflows.PortfolioReview.Messages;
using InvestissementsDashboard.Shared.Models;
using Microsoft.Agents.AI.Workflows;

namespace AgentAI.Core.Workflows.PortfolioReview.Executors;

/// <summary>
/// Reads the positions and the watchlist from the Api. Deliberately without a model: an agent asked to copy this data
/// dropped many positions, invented duplicates and reported the wrong total, which distorted the synthesis.
/// </summary>
internal sealed class AllocationExecutor(IAllocationSource source) : Executor<AnalysisRequest, AllocationResult>("allocation")
{
    public override async ValueTask<AllocationResult> HandleAsync(AnalysisRequest message, IWorkflowContext context, CancellationToken cancellationToken = default)
    {
        var assetsTask = source.GetAssetsAsync(cancellationToken);
        var snapshotTask = source.GetLastSnapshotAsync(cancellationToken);
        var watchlistTask = source.GetWatchlistAsync(cancellationToken);
        await Task.WhenAll(assetsTask, snapshotTask, watchlistTask);

        var lines = (await assetsTask)
            // Sold positions and the sheet's placeholder rows say nothing about the portfolio; a lost position does.
            .Where(asset => asset.CurrentTotal is > 0m || asset.Roi is < 0m)
            .Select(ToLine)
            .OrderByDescending(line => line.Value)
            .ToList();

        var allocation = new AllocationResult(
            // The date of the data, which may precede the requested one until the next synchronisation.
            (await snapshotTask)?.Date ?? message.Date,
            lines.Sum(line => line.Value),
            lines,
            await watchlistTask);

        await context.AddEventAsync(new AllocationRetrievedEvent(allocation), cancellationToken);
        return allocation;
    }

    private static AllocationLine ToLine(AssetDto asset) => new(
        asset.Name,
        string.IsNullOrEmpty(asset.Isin) ? null : asset.Isin,
        asset.AssetClass,
        asset.Sector,
        asset.Geography,
        asset.CurrentTotal ?? 0m,
        asset.WeightInPortfolio,
        asset.UnrealizedGain,
        asset.Roi);
}

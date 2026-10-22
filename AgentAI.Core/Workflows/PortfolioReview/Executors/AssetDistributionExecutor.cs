using Microsoft.Agents.AI.Workflows;

namespace AgentAI;

/// <summary>
/// Gathers the geographic and sector distributions of the stocks and bonds, straight from the dashboard Api:
/// no agent is involved, so the figures are exactly those the dashboard shows.
/// </summary>
public sealed class AssetDistributionExecutor(IAssetDistributionSource source) : Executor<AnalysisRequest, DistributionResult>("distribution")
{
    // The only classes the Api distributes by geography and sector.
    private static readonly string[] AssetClasses = ["Stocks", "Bonds"];

    public override async ValueTask<DistributionResult> HandleAsync(AnalysisRequest message, IWorkflowContext context, CancellationToken cancellationToken = default)
    {
        // The four Api calls are independent: running them together keeps the step as short as its slowest call.
        var classes = await Task.WhenAll(AssetClasses.Select(assetClass => this.GetClassAsync(assetClass, cancellationToken)));
        var distribution = new DistributionResult(classes);

        await context.AddEventAsync(new AssetDistributionRetrievedEvent(distribution), cancellationToken);
        return distribution;
    }

    private async Task<AssetClassDistribution> GetClassAsync(string assetClass, CancellationToken cancellationToken)
    {
        var geographyTask = source.GetGeographyAsync(assetClass, cancellationToken);
        var sectorsTask = source.GetSectorsAsync(assetClass, cancellationToken);
        await Task.WhenAll(geographyTask, sectorsTask);

        return new AssetClassDistribution(assetClass, await geographyTask, await sectorsTask);
    }
}

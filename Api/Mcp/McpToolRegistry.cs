using InvestissementsDashboard.Shared.Mcp;

namespace InvestissementsDashboard.Api.Mcp;

internal static class McpToolRegistry
{
    public static readonly IReadOnlyList<McpToolDefinition> Tools = BuildTools();

    private static IReadOnlyList<McpToolDefinition> BuildTools() =>
    [
        new("get_assets",
            "Returns all assets in the investment portfolio.",
            NoParams()),

        new("get_assets_distribution",
            "Returns the portfolio distribution grouped by a given dimension.",
            EnumParam("dimension", "Grouping dimension",
                ["assetClass", "assetType", "supportType", "support"])),

        new("get_etf_stocks",
            "Returns ETF/stock aggregates grouped by information category.",
            NoParams()),

        new("get_portfolio_metrics",
            "Returns portfolio-level metrics: ROI on purchases, ROI on capital engaged, average risk.",
            NoParams()),

        new("get_portfolio_history",
            "Returns indexed performance history comparing portfolio vs benchmarks (LifeStrategy 60, MSCI World).",
            NoParams()),

        new("get_snapshot",
            "Returns the most recent portfolio snapshot.",
            NoParams()),

        new("get_snapshot_history",
            "Returns the full history of daily portfolio snapshots.",
            NoParams()),

        new("get_geography_distribution",
            "Returns geographic distribution of assets for a given asset class.",
            EnumParam("assetClass", "Asset class to filter by", ["Stocks", "Bonds"])),
    ];

    private static object NoParams() => new
    {
        type = "object",
        properties = new { },
        required = Array.Empty<string>()
    };

    private static object EnumParam(string name, string description, string[] values) => new
    {
        type = "object",
        properties = new Dictionary<string, object>
        {
            [name] = new { type = "string", description, @enum = values }
        },
        required = new[] { name }
    };
}

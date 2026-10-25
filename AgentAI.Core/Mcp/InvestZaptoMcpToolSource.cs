using Microsoft.Extensions.AI;
using ModelContextProtocol.Client;

namespace AgentAI;

/// <summary>
/// Provides the tools of the InvestZapto MCP server, connecting on first use and owning that connection.
/// </summary>
/// <remarks>
/// Connecting lazily keeps the agents that do not read the portfolio from requiring the MCP URL or reaching the
/// server. The connection is kept for later agents built by the same factory, then released by <see cref="DisposeAsync"/>.
/// A failed connection is not kept, so the next call tries again.
/// </remarks>
internal sealed class InvestZaptoMcpToolSource(InvestZaptoMcpClient connector, string? endpoint) : IAsyncDisposable
{
    private McpClient? _client;

    public async Task<IList<AITool>> GetToolsAsync(CancellationToken cancellationToken = default)
    {
        var url = endpoint ?? throw new InvalidOperationException($"{nameof(AgentAIOptions.InvestZaptoMcpUrl)} is not set.");

        this._client ??= await connector.ConnectAsync(url, cancellationToken).ConfigureAwait(false);
        var tools = await this._client.ListToolsAsync(cancellationToken: cancellationToken).ConfigureAwait(false);

        return tools.ToList<AITool>();
    }

    public async ValueTask DisposeAsync()
    {
        if (this._client is not null)
        {
            await this._client.DisposeAsync().ConfigureAwait(false);
            this._client = null;
        }
    }
}

using Microsoft.Extensions.Logging;
using ModelContextProtocol.Client;

namespace AgentAI;

/// <summary>
/// Connects to the InvestZapto MCP server over HTTP.
/// </summary>
/// <remarks>
/// The server only implements the Streamable HTTP transport (no legacy HTTP+SSE), so
/// <see cref="HttpTransportMode.StreamableHttp"/> is forced explicitly: the default
/// <see cref="HttpTransportMode.AutoDetect"/> would otherwise fall back to a legacy SSE probe that
/// this server always rejects with 404, turning any transient Streamable HTTP hiccup into a confusing
/// dual-error stack trace instead of a clean retry.
/// The <see cref="HttpClient"/> is injected (and owned by the host) so this class carries no
/// platform-specific handler and can be reused from a browser host.
/// </remarks>
public class InvestZaptoMcpClient
{
    #region Fields
    private const int MaxAttempts = 3;
    private static readonly TimeSpan RetryDelay = TimeSpan.FromSeconds(2);

    private readonly HttpClient _httpClient;
    private readonly ILoggerFactory _loggerFactory;
    private readonly ILogger<InvestZaptoMcpClient> _logger;
    #endregion

    #region Constructor
    public InvestZaptoMcpClient(HttpClient httpClient, ILoggerFactory loggerFactory)
    {
        ArgumentNullException.ThrowIfNull(httpClient);
        ArgumentNullException.ThrowIfNull(loggerFactory);

        _httpClient = httpClient;
        _loggerFactory = loggerFactory;
        _logger = loggerFactory.CreateLogger<InvestZaptoMcpClient>();
    }
    #endregion

    #region Methods
    public async Task<McpClient> ConnectAsync(string endpoint, CancellationToken cancellationToken = default)
    {
        var transportOptions = new HttpClientTransportOptions
        {
            Endpoint = new Uri(endpoint),
            TransportMode = HttpTransportMode.StreamableHttp, // Force Streamable HTTP to avoid fallback to legacy SSE probe that the server rejects with 404.
        };

        for (var attempt = 1; ; attempt++)
        {
            try
            {
                // ownsHttpClient: false because the host shares this HttpClient across attempts and callers.
                var transport = new HttpClientTransport(transportOptions, _httpClient, _loggerFactory, ownsHttpClient: false);
                return await McpClient.CreateAsync(transport, cancellationToken: cancellationToken).ConfigureAwait(false);
            }
            catch (HttpRequestException ex) when (attempt < MaxAttempts)
            {
                _logger.LogWarning(
                    ex,
                    "Connection to {Endpoint} failed (attempt {Attempt}/{MaxAttempts}). Retrying in {RetryDelaySeconds}s.",
                    endpoint,
                    attempt,
                    MaxAttempts,
                    RetryDelay.TotalSeconds);
                await Task.Delay(RetryDelay, cancellationToken).ConfigureAwait(false);
            }
        }
    }
    #endregion
}


namespace AgentAI;

/// <summary>
/// Buffers outgoing request bodies so they are sent with an explicit <c>Content-Length</c> instead
/// of <c>Transfer-Encoding: chunked</c> (the MCP SDK's request content does not set a length up
/// front). The InvestZapto backend fails every chunked POST with a generic
/// <c>500 Backend call failure</c>, while an identical request with a known length succeeds.
/// </summary>
/// <remarks>
/// The inner handler is left to the caller (<see cref="DelegatingHandler.InnerHandler"/>) so this
/// class stays platform-neutral: native hosts plug a <c>SocketsHttpHandler</c>, while a browser host
/// (Blazor WASM) can plug its own handler or skip this workaround altogether.
/// </remarks>
public sealed class ForceContentLengthHandler : DelegatingHandler
{
    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        if (request.Content is not null && request.Content.Headers.ContentLength is null)
        {
            var bytes = await request.Content.ReadAsByteArrayAsync(cancellationToken).ConfigureAwait(false);
            var contentType = request.Content.Headers.ContentType;
            var bufferedContent = new ByteArrayContent(bytes);
            if (contentType is not null)
            {
                bufferedContent.Headers.ContentType = contentType;
            }

            request.Content = bufferedContent;
        }

        return await base.SendAsync(request, cancellationToken).ConfigureAwait(false);
    }
}

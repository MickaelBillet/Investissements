namespace AgentAI;

/// <summary>
/// Reads unofficial RSS endpoints over HTTP; parsing is delegated to <see cref="RssFeedParser"/>.
/// </summary>
public sealed class RssFeedReader : IRssFeedReader
{
    private readonly HttpClient _httpClient;

    public RssFeedReader(HttpClient httpClient)
    {
        ArgumentNullException.ThrowIfNull(httpClient);

        _httpClient = httpClient;
    }

    public async Task<IReadOnlyList<NewsArticle>> ReadAsync(Uri url, string defaultSource, CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, url);

        // Some feed endpoints reject requests that carry no User-Agent.
        request.Headers.UserAgent.ParseAdd("Mozilla/5.0 (compatible; AgentAI)");

        using var response = await _httpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();

        var content = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
        return RssFeedParser.Parse(content, defaultSource);
    }
}

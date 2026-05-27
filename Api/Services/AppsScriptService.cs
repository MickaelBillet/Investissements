using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using InvestissementsDashboard.Api.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace InvestissementsDashboard.Api.Services;

internal sealed class AppsScriptService : IAppsScriptService
{
    private const string AppsScriptUrlKey    = "APPS_SCRIPT_URL";
    private const string AppsScriptApiKeyKey = "APPS_SCRIPT_API_KEY";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        NumberHandling = JsonNumberHandling.AllowReadingFromString,
        Converters = { new FlexibleIntConverter(), new FlexibleStringConverter() }
    };

    private readonly HttpClient _httpClient;
    private readonly string _baseUrl;
    private readonly string _apiKey;
    private readonly ILogger<AppsScriptService> _logger;

    public AppsScriptService(HttpClient httpClient, IConfiguration configuration, ILogger<AppsScriptService> logger)
    {
        _httpClient = httpClient;
        _logger     = logger;
        _baseUrl    = configuration[AppsScriptUrlKey]    ?? throw new InvalidOperationException($"{AppsScriptUrlKey} is not configured.");
        _apiKey     = configuration[AppsScriptApiKeyKey] ?? throw new InvalidOperationException($"{AppsScriptApiKeyKey} is not configured.");
    }

    public Task<T?> CallAsync<T>(string service, string action, CancellationToken ct = default)
        => CallAsync<T>(service, action, null, ct);

    public async Task<T?> CallAsync<T>(string service, string action, IReadOnlyDictionary<string, string>? extraParams, CancellationToken ct = default)
    {
        var response = await _httpClient.GetAsync(BuildRequestUrl(service, action, extraParams), ct);
        EnsureHttpSuccess(response, service, action);
        var content = await response.Content.ReadAsStringAsync(ct);
        ThrowIfAppsScriptError(content, service, action);
        return JsonSerializer.Deserialize<T>(content, JsonOptions);
    }

    private string BuildRequestUrl(string service, string action, IReadOnlyDictionary<string, string>? extraParams = null)
    {
        var sb = new StringBuilder($"{_baseUrl}?apiKey={_apiKey}&service={service}&action={action}");
        if (extraParams is not null)
            foreach (var (k, v) in extraParams)
                sb.Append($"&{Uri.EscapeDataString(k)}={Uri.EscapeDataString(v)}");
        return sb.ToString();
    }

    private void EnsureHttpSuccess(HttpResponseMessage response, string service, string action)
    {
        if (response.IsSuccessStatusCode) return;
        _logger.LogError("Apps Script returned {StatusCode} for service='{Service}' action='{Action}'.",
            response.StatusCode, service, action);
        response.EnsureSuccessStatusCode();
    }

    private void ThrowIfAppsScriptError(string content, string service, string action)
    {
        using var doc = JsonDocument.Parse(content);
        if (doc.RootElement.ValueKind != JsonValueKind.Object) return;
        if (!doc.RootElement.TryGetProperty("error", out var error)) return;

        var msg = error.GetString();
        _logger.LogError("Apps Script returned error for service='{Service}' action='{Action}': {Error}.",
            service, action, msg);
        throw new InvalidOperationException($"Apps Script error: {msg}");
    }
}

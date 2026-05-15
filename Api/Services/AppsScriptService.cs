using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace InvestissementsDashboard.Api.Services;

internal sealed class AppsScriptService : IAppsScriptService
{
    // Google Sheets can return any field as a number instead of a string.
    // This converter handles all string fields robustly.
    private sealed class FlexibleStringConverter : JsonConverter<string>
    {
        public override string? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
            => reader.TokenType switch
            {
                JsonTokenType.String => reader.GetString(),
                JsonTokenType.Number => reader.TryGetInt64(out var l)
                                        ? l.ToString()
                                        : reader.GetDouble().ToString(CultureInfo.InvariantCulture),
                JsonTokenType.True   => "true",
                JsonTokenType.False  => "false",
                JsonTokenType.Null   => null,
                _                   => null
            };

        public override void Write(Utf8JsonWriter writer, string value, JsonSerializerOptions options)
            => writer.WriteStringValue(value);
    }

    // Google Sheets can return numeric IDs as strings or floats (e.g. "5.0").
    // This converter handles all int/int? fields robustly.
    private sealed class FlexibleIntConverter : JsonConverter<int>
    {
        public override int Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType == JsonTokenType.Null) return 0;
            if (reader.TokenType == JsonTokenType.Number)
            {
                if (reader.TryGetInt32(out var i)) return i;
                if (reader.TryGetDouble(out var d)) return (int)d;
            }
            if (reader.TokenType == JsonTokenType.String)
            {
                var s = reader.GetString();
                if (double.TryParse(s, System.Globalization.NumberStyles.Any,
                        System.Globalization.CultureInfo.InvariantCulture, out var d))
                    return (int)d;
            }
            throw new JsonException($"Cannot convert token '{reader.TokenType}' to Int32.");
        }

        public override void Write(Utf8JsonWriter writer, int value, JsonSerializerOptions options)
            => writer.WriteNumberValue(value);
    }

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

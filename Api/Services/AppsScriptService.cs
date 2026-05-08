using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace InvestissementsDashboard.Api.Services;

internal sealed class AppsScriptService : IAppsScriptService
{
    // Google Sheets can return numeric IDs as strings or floats (e.g. "5.0").
    // This converter handles all int/int? fields robustly.
    private sealed class FlexibleIntConverter : JsonConverter<int>
    {
        public override int Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
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

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        NumberHandling = JsonNumberHandling.AllowReadingFromString,
        Converters = { new FlexibleIntConverter() }
    };

    private readonly HttpClient _httpClient;
    private readonly string _baseUrl;
    private readonly string _apiKey;
    private readonly ILogger<AppsScriptService> _logger;

    public AppsScriptService(HttpClient httpClient, IConfiguration configuration, ILogger<AppsScriptService> logger)
    {
        _httpClient = httpClient;
        _logger     = logger;
        _baseUrl    = configuration["APPS_SCRIPT_URL"]     ?? throw new InvalidOperationException("APPS_SCRIPT_URL is not configured.");
        _apiKey     = configuration["APPS_SCRIPT_API_KEY"] ?? throw new InvalidOperationException("APPS_SCRIPT_API_KEY is not configured.");
    }

    public async Task<T?> CallAsync<T>(string service, string action, CancellationToken ct = default)
    {
        var url = $"{_baseUrl}?apiKey={_apiKey}&service={service}&action={action}";

        var response = await _httpClient.GetAsync(url, ct);

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogError("Apps Script returned {StatusCode} for service='{Service}' action='{Action}'.", response.StatusCode, service, action);
            response.EnsureSuccessStatusCode();
        }

        var content = await response.Content.ReadAsStringAsync(ct);

        using var doc = JsonDocument.Parse(content);
        if (doc.RootElement.ValueKind == JsonValueKind.Object &&
            doc.RootElement.TryGetProperty("error", out var error))
        {
            var msg = error.GetString();
            _logger.LogError("Apps Script returned error for service='{Service}' action='{Action}': {Error}.", service, action, msg);
            throw new InvalidOperationException($"Apps Script error: {msg}");
        }

        return JsonSerializer.Deserialize<T>(content, JsonOptions);
    }
}

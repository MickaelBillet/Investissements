using System.Text.Json;
using InvestissementsDashboard.Shared.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace InvestissementsDashboard.Api.Services;

internal sealed class SyncService : ISyncService
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;
    private readonly ILogger<SyncService> _logger;

    public SyncService(HttpClient httpClient, IConfiguration configuration, ILogger<SyncService> logger)
    {
        _httpClient    = httpClient;
        _configuration = configuration;
        _logger        = logger;
    }

    public async Task<SyncResultDto> TriggerAsync(CancellationToken ct = default)
    {
        var baseUrl = _configuration["APPS_SCRIPT_SYNC_URL"];
        var key     = _configuration["APPS_SCRIPT_SYNC_KEY"];

        if (string.IsNullOrEmpty(baseUrl) || string.IsNullOrEmpty(key))
        {
            _logger.LogError("APPS_SCRIPT_SYNC_URL or APPS_SCRIPT_SYNC_KEY is not configured.");
            return new SyncResultDto(false, 0, "Synchronisation non configurée.");
        }

        try
        {
            var url = $"{baseUrl}?key={Uri.EscapeDataString(key)}";
            var response = await _httpClient.GetAsync(url, ct);
            var body = await response.Content.ReadAsStringAsync(ct);

            AppsScriptSyncResponse? payload;
            try
            {
                payload = JsonSerializer.Deserialize<AppsScriptSyncResponse>(body, JsonOptions);
            }
            catch (JsonException)
            {
                _logger.LogError("Apps Script sync returned a non-JSON response ({StatusCode}): {Body}",
                    (int)response.StatusCode, Truncate(body));
                return new SyncResultDto(false, 0, "Réponse invalide de Google Apps Script — vérifie le déploiement du Web App.");
            }

            if (!response.IsSuccessStatusCode || payload is null || !payload.Success)
            {
                _logger.LogError("Apps Script sync failed: {Error}", payload?.Error ?? "unknown error");
                return new SyncResultDto(false, 0, payload?.Error ?? "Erreur inconnue.");
            }

            return new SyncResultDto(true, payload.AddedCount, null);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            _logger.LogError(ex, "Failed to call the Apps Script sync endpoint.");
            return new SyncResultDto(false, 0, "Impossible de contacter Google Apps Script.");
        }
    }

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private static string Truncate(string value, int maxLength = 500)
        => value.Length <= maxLength ? value : value[..maxLength] + "…";

    private sealed record AppsScriptSyncResponse(bool Success, int AddedCount, string? Error);
}

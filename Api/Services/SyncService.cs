using System.Net.Http.Json;
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
            var payload = await response.Content.ReadFromJsonAsync<AppsScriptSyncResponse>(cancellationToken: ct);

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

    private sealed record AppsScriptSyncResponse(bool Success, int AddedCount, string? Error);
}

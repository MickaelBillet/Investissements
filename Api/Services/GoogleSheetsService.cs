using System.Net.Http.Json;
using InvestissementsDashboard.Api.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace InvestissementsDashboard.Api.Services;

internal sealed class GoogleSheetsService : IGoogleSheetsService
{
    private readonly HttpClient _httpClient;
    private readonly string _sheetId;
    private readonly string _apiKey;
    private readonly ILogger<GoogleSheetsService> _logger;

    public GoogleSheetsService(HttpClient httpClient, IConfiguration configuration, ILogger<GoogleSheetsService> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
        _sheetId = configuration["GOOGLE_SHEET_ID"] ?? throw new InvalidOperationException("GOOGLE_SHEET_ID is not configured.");
        _apiKey = configuration["GOOGLE_SHEETS_API_KEY"] ?? throw new InvalidOperationException("GOOGLE_SHEETS_API_KEY is not configured.");
    }

    public async Task<IReadOnlyList<IReadOnlyList<string>>> GetRangeAsync(string range, CancellationToken ct = default)
    {
        var url = $"https://sheets.googleapis.com/v4/spreadsheets/{_sheetId}/values/{Uri.EscapeDataString(range)}?key={_apiKey}";

        var response = await _httpClient.GetAsync(url, ct);

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogError("Google Sheets API returned {StatusCode} for range '{Range}'.", response.StatusCode, range);
            response.EnsureSuccessStatusCode();
        }

        var result = await response.Content.ReadFromJsonAsync<SheetValuesResponse>(ct);

        return result?.Values ?? [];
    }
}

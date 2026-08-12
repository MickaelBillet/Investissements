using Google.Apis.Auth.OAuth2;
using Google.Apis.Services;
using Google.Apis.Sheets.v4;
using Microsoft.Extensions.Configuration;

namespace InvestissementsDashboard.GoogleSheets;

public sealed class GoogleSheetsClient : IGoogleSheetsClient, IDisposable
{
    private const string SheetIdKey             = "GOOGLE_SHEET_ID";
    private const string ServiceAccountEmailKey  = "GOOGLE_SERVICE_ACCOUNT_EMAIL";
    private const string ServiceAccountKeyKey    = "GOOGLE_SERVICE_ACCOUNT_KEY";

    private readonly SheetsService _sheetsService;
    private readonly string _sheetId;

    public GoogleSheetsClient(IConfiguration configuration)
    {
        _sheetId = configuration[SheetIdKey]
            ?? throw new InvalidOperationException($"{SheetIdKey} is not configured.");
        var email = configuration[ServiceAccountEmailKey]
            ?? throw new InvalidOperationException($"{ServiceAccountEmailKey} is not configured.");
        var privateKey = configuration[ServiceAccountKeyKey]
            ?? throw new InvalidOperationException($"{ServiceAccountKeyKey} is not configured.");

        // App settings store the PEM key as a single line with literal "\n" sequences.
        var credential = new ServiceAccountCredential(
            new ServiceAccountCredential.Initializer(email)
            {
                Scopes = [SheetsService.Scope.SpreadsheetsReadonly]
            }.FromPrivateKey(privateKey.Replace("\\n", "\n")));

        _sheetsService = new SheetsService(new BaseClientService.Initializer
        {
            HttpClientInitializer = credential,
            ApplicationName       = "InvestissementsDashboard"
        });
    }

    public async Task<IReadOnlyList<IReadOnlyList<object>>> GetRangeAsync(string sheetName, CancellationToken ct = default)
    {
        var request = _sheetsService.Spreadsheets.Values.Get(_sheetId, $"{sheetName}!A:Z");
        // Raw typed values (numbers as numbers, no locale/currency formatting) — mirrors
        // what Apps Script's getValues() returns, unlike the API's formatted-string default.
        request.ValueRenderOption = SpreadsheetsResource.ValuesResource.GetRequest.ValueRenderOptionEnum.UNFORMATTEDVALUE;

        var response = await request.ExecuteAsync(ct);

        return response.Values?
            .Select(row => (IReadOnlyList<object>)row.ToList())
            .ToList()
            ?? [];
    }

    public void Dispose() => _sheetsService.Dispose();
}

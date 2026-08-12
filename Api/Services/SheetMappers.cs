using InvestissementsDashboard.Shared.Models;

namespace InvestissementsDashboard.Api.Services;

// Pure row-to-DTO mapping and aggregation, ported from Scripts/Router.gs, AssetService.gs,
// AssetTypeService.gs and SnapshotService.gs — same column layout as Scripts/Config.gs.
internal static class SheetMappers
{
    // --- Asset sheet column indexes (0-based), mirrors Scripts/Config.gs ---
    private const int ColId             = 0;
    private const int ColName           = 1;
    private const int ColAssetClass     = 2;
    private const int ColSupportType    = 3;
    private const int ColSupport        = 4;
    private const int ColAssetType      = 5;
    private const int ColSector         = 6;
    private const int ColInformation    = 7;
    private const int ColGeography      = 8;
    private const int ColRisk           = 9;
    private const int ColTotalPurchases = 10;
    private const int ColTotalSales     = 11;
    private const int ColDividends      = 12;
    private const int ColCurrentTotal   = 13;

    // --- Snapshot sheet column indexes ---
    private const int ColSnapDate           = 0;
    private const int ColSnapNetCapital     = 1;
    private const int ColSnapLifeStrategy   = 2;
    private const int ColSnapMsciWorld      = 3;
    private const int ColSnapTotalPurchases = 4;
    private const int ColSnapTotalReturns   = 5;
    private const int ColSnapTotalSales     = 6;

    // --- AssetType sheet column indexes ---
    private const int ColAtId                = 0;
    private const int ColAtName              = 1;
    private const int ColAtLabelFr           = 3;
    private const int ColAtGeoSectorEligible = 4;

    // --- Row filtering (skip header, skip "Not Defined" rows) ---

    public static IReadOnlyList<IReadOnlyList<object>> GetAssetRows(IReadOnlyList<IReadOnlyList<object>> rawRows)
        => rawRows.Skip(1).Where(row => AsString(Cell(row, ColName)) != "Not Defined").ToList();

    public static IReadOnlyList<IReadOnlyList<object>> GetAssetTypeRows(IReadOnlyList<IReadOnlyList<object>> rawRows)
        => rawRows.Skip(1).Where(row => !string.IsNullOrEmpty(AsString(Cell(row, ColAtName)))).ToList();

    public static IReadOnlyList<IReadOnlyList<object>> GetSnapshotRows(IReadOnlyList<IReadOnlyList<object>> rawRows)
        => rawRows.Skip(1).Where(row => !string.IsNullOrEmpty(AsString(Cell(row, ColSnapDate)))).ToList();

    // --- Asset ---

    public static decimal GetPortfolioTotal(IReadOnlyList<IReadOnlyList<object>> rows)
        => rows.Sum(row => IsNd(Cell(row, ColCurrentTotal)) ? 0m : AsDecimalOrZero(Cell(row, ColCurrentTotal)));

    public static AssetDto BuildAssetRow(IReadOnlyList<object> row, decimal portfolioTotal)
    {
        var totalPurchases = Cell(row, ColTotalPurchases);
        var totalSales      = Cell(row, ColTotalSales);
        var dividends        = Cell(row, ColDividends);
        var currentTotalCell = Cell(row, ColCurrentTotal);

        var hasFinancialData = IsTruthy(totalPurchases) && !IsNd(totalPurchases) && !IsNd(totalSales);

        var ct         = AsDecimalOrNull(currentTotalCell);
        var hasCurrent = ct is not null;

        var tp = hasFinancialData ? AsDecimalOrZero(totalPurchases) : 0m;
        var ts = hasFinancialData ? AsDecimalOrZero(totalSales) : 0m;
        var div = hasFinancialData ? AsDecimalOrZero(dividends) : 0m;
        var netInvested = tp - ts;

        return new AssetDto(
            Id            : (int)AsDecimalOrZero(Cell(row, ColId)),
            Name          : AsString(Cell(row, ColName)) ?? "",
            AssetClass    : AsString(Cell(row, ColAssetClass)) ?? "",
            SupportType   : AsString(Cell(row, ColSupportType)) ?? "",
            Support       : AsString(Cell(row, ColSupport)) ?? "",
            AssetType     : AsString(Cell(row, ColAssetType)) ?? "",
            Sector        : AsString(Cell(row, ColSector)) ?? "",
            Information   : AsString(Cell(row, ColInformation)) ?? "",
            Geography     : AsString(Cell(row, ColGeography)) ?? "",
            Risk          : (int)AsDecimalOrZero(Cell(row, ColRisk)),
            TotalPurchases: hasFinancialData ? tp : null,
            TotalSales    : hasFinancialData ? ts : null,
            Dividends     : hasFinancialData ? div : null,
            CurrentTotal  : ct,
            UnrealizedGain: hasFinancialData && hasCurrent && netInvested != 0 ? ct!.Value - netInvested : null,
            Yield         : hasFinancialData && netInvested != 0 ? Math.Round(div / netInvested * 10000m, 0) / 100m : null,
            Roi           : hasFinancialData && tp != 0 ? Math.Round((ct.GetValueOrDefault() + ts + div - tp) / tp * 10000m, 0) / 100m : null,
            WeightInPortfolio: portfolioTotal != 0 ? Math.Round(AsDecimalOrZero(currentTotalCell) / portfolioTotal * 10000m, 0) / 100m : 0m
        );
    }

    // --- Snapshot ---

    public static SnapshotDto BuildSnapshotRow(IReadOnlyList<object> row) => new(
        Date          : AsDate(Cell(row, ColSnapDate)),
        NetCapital    : AsDecimalOrZero(Cell(row, ColSnapNetCapital)),
        LifeStrategy  : AsDecimalOrNull(Cell(row, ColSnapLifeStrategy)),
        MsciWorld     : AsDecimalOrNull(Cell(row, ColSnapMsciWorld)),
        TotalPurchases: AsDecimalOrZero(Cell(row, ColSnapTotalPurchases)),
        TotalReturns  : AsDecimalOrZero(Cell(row, ColSnapTotalReturns)),
        TotalSales    : AsDecimalOrNull(Cell(row, ColSnapTotalSales))
    );

    // --- AssetType reference ---

    public static AssetTypeReferenceDto BuildAssetTypeReference(IReadOnlyList<object> row)
    {
        var labelFr = AsString(Cell(row, ColAtLabelFr));
        var geo     = Cell(row, ColAtGeoSectorEligible);

        return new AssetTypeReferenceDto(
            Id               : AsDecimalOrNull(Cell(row, ColAtId)) is { } id ? (int)id : null,
            Name             : AsString(Cell(row, ColAtName)) ?? "",
            LabelFr          : string.IsNullOrEmpty(labelFr) ? null : labelFr,
            GeoSectorEligible: geo is bool b
                ? b
                : string.Equals(AsString(geo)?.Trim(), "TRUE", StringComparison.OrdinalIgnoreCase)
                  || string.Equals(AsString(geo)?.Trim(), "OUI", StringComparison.OrdinalIgnoreCase)
        );
    }

    // --- Grouping / aggregation (Router.gs: groupBy, sumColumn, aggregateGroup) ---

    public static IReadOnlyList<DistributionDto> GetDistributionByColumn(
        IReadOnlyList<IReadOnlyList<object>> rows, int colIndex, decimal portfolioTotal)
        => GroupBy(rows, colIndex).Select(g =>
        {
            var currentTotal = SumColumn(g.Value, ColCurrentTotal);
            return new DistributionDto(
                Name             : g.Key,
                CurrentTotal     : currentTotal,
                WeightInPortfolio: portfolioTotal != 0 ? Math.Round(currentTotal / portfolioTotal * 10000m, 0) / 100m : 0m,
                Id               : null);
        }).ToList();

    public static AggregateDto AggregateGroup(
        string name, IReadOnlyList<IReadOnlyList<object>> rows, decimal groupTotal, decimal portfolioTotal)
    {
        decimal totalPurchases = 0, totalSales = 0, dividends = 0, currentTotal = 0;
        var hasNd = false;

        foreach (var row in rows)
        {
            var tp = Cell(row, ColTotalPurchases);
            var ts = Cell(row, ColTotalSales);
            var div = Cell(row, ColDividends);
            var ct = Cell(row, ColCurrentTotal);

            if (IsNd(tp)) hasNd = true;

            totalPurchases += IsTruthy(tp) && !IsNd(tp) ? AsDecimalOrZero(tp) : 0m;
            totalSales     += IsTruthy(ts) && !IsNd(ts) ? AsDecimalOrZero(ts) : 0m;
            dividends      += IsTruthy(div) && !IsNd(div) ? AsDecimalOrZero(div) : 0m;
            currentTotal   += IsTruthy(ct) && !IsNd(ct) ? AsDecimalOrZero(ct) : 0m;
        }

        var netInvested = totalPurchases - totalSales;

        return new AggregateDto(
            Name             : name,
            TotalPurchases   : totalPurchases,
            TotalSales       : totalSales,
            Dividends        : dividends,
            CurrentTotal     : currentTotal,
            HasIncompleteData: hasNd,
            UnrealizedGain   : !hasNd && netInvested != 0 ? currentTotal - netInvested : null,
            Yield            : !hasNd && netInvested != 0 ? Math.Round(dividends / netInvested * 10000m, 0) / 100m : null,
            Roi              : !hasNd && totalPurchases != 0 ? Math.Round((currentTotal + totalSales + dividends - totalPurchases) / totalPurchases * 10000m, 0) / 100m : null,
            WeightInGroup    : groupTotal != 0 ? Math.Round(currentTotal / groupTotal * 10000m, 0) / 100m : 0m,
            WeightInPortfolio: portfolioTotal != 0 ? Math.Round(currentTotal / portfolioTotal * 10000m, 0) / 100m : 0m
        );
    }

    public static IReadOnlyDictionary<string, List<IReadOnlyList<object>>> GroupBy(
        IReadOnlyList<IReadOnlyList<object>> rows, int colIndex)
    {
        var acc = new Dictionary<string, List<IReadOnlyList<object>>>();
        foreach (var row in rows)
        {
            var key = AsString(Cell(row, colIndex)) ?? "";
            if (!acc.TryGetValue(key, out var list))
                acc[key] = list = [];
            list.Add(row);
        }
        return acc;
    }

    public static decimal SumColumn(IReadOnlyList<IReadOnlyList<object>> rows, int colIndex)
        => rows.Sum(row => AsDecimalOrZero(Cell(row, colIndex)));

    // --- Column indexes used by AssetsService for the distribution-by-dimension endpoint ---
    public static int AssetClassColumn  => ColAssetClass;
    public static int SupportTypeColumn => ColSupportType;
    public static int SupportColumn     => ColSupport;
    public static int AssetTypeColumn   => ColAssetType;
    public static int InformationColumn => ColInformation;
    public static int CurrentTotalColumn => ColCurrentTotal;

    // --- Cell helpers ---

    private static object? Cell(IReadOnlyList<object> row, int index) => index < row.Count ? row[index] : null;

    private static bool IsNd(object? v) => v is string s && s == "ND";

    private static bool IsTruthy(object? v) => v switch
    {
        null   => false,
        string s => s.Length > 0,
        double d => d != 0,
        bool b   => b,
        _        => true
    };

    private static string? AsString(object? v) => v?.ToString();

    // Google Sheets stores date-looking text as a real date internally, so with
    // UNFORMATTED_VALUE it comes back as a serial number (days since 1899-12-30),
    // not the "yyyy-MM-dd" string the ETL originally wrote.
    private static readonly DateTime SheetsEpoch = new(1899, 12, 30);

    private static DateOnly AsDate(object? v) => v switch
    {
        double d => DateOnly.FromDateTime(SheetsEpoch.AddDays(d)),
        long l   => DateOnly.FromDateTime(SheetsEpoch.AddDays(l)),
        int i    => DateOnly.FromDateTime(SheetsEpoch.AddDays(i)),
        _        => DateOnly.Parse(AsString(v)!)
    };

    private static decimal AsDecimalOrZero(object? v) => AsDecimalOrNull(v) ?? 0m;

    private static decimal? AsDecimalOrNull(object? v) => v switch
    {
        null           => null,
        double d       => (decimal)d,
        long l         => l,
        int i          => i,
        string s when decimal.TryParse(s, out var d) => d,
        _              => null
    };
}

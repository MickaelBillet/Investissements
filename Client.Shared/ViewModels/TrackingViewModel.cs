using InvestissementsDashboard.Client.Model;
using InvestissementsDashboard.Client.Services;
using InvestissementsDashboard.Shared.Models;

namespace InvestissementsDashboard.Client.ViewModels;

public class TrackingViewModel(IPortfolioService portfolioService, ILocalizationService localizationService)
{
    public bool    IsLoading         { get; private set; } = true;
    public string? HistoryError      { get; private set; }
    public string? BondScheduleError { get; private set; }

    public IReadOnlyList<IndexedPoint> ROIC_Series { get; private set; } = [];
    public IReadOnlyList<IndexedPoint> LifeStrategySeries { get; private set; } = [];
    public IReadOnlyList<IndexedPoint> MsciWorldSeries    { get; private set; } = [];
    public IReadOnlyList<BondScheduleDto> BondSchedule     { get; private set; } = [];
    private bool _bondScheduleQuarterlyView;

    // Changing the granularity invalidates the selected period (a quarter label means nothing in the yearly view).
    public bool BondScheduleQuarterlyView
    {
        get => _bondScheduleQuarterlyView;
        set
        {
            if (_bondScheduleQuarterlyView == value) return;
            _bondScheduleQuarterlyView = value;
            SelectedPeriodEntry = null;
        }
    }

    public BondSchedulePeriodDto? SelectedPeriodEntry { get; private set; }

    // Zero-value bonds are already repaid/sold: hiding them keeps the list focused on live positions.
    public IReadOnlyList<BondScheduleItemDto> SelectedPeriodBonds =>
        SelectedPeriodEntry is null
            ? []
            : [.. SelectedPeriodEntry.Bonds.Where(b => b.Amount > 0).OrderByDescending(b => b.Amount)];

    public void SelectPeriod(BondSchedulePeriodDto entry) => SelectedPeriodEntry = entry;

    public IReadOnlyList<BondSchedulePeriodDto> BondScheduleDisplayed =>
        BondScheduleQuarterlyView
            ? BondSchedule
                .GroupBy(s => (s.Year, Quarter: (s.Month - 1) / 3 + 1))
                .Select(g => new BondSchedulePeriodDto(g.Key.Year, g.Key.Quarter, g.Sum(s => s.Amount), [.. g.SelectMany(s => s.Bonds)]))
                .OrderBy(p => p.Year).ThenBy(p => p.Quarter)
                .ToArray()
            : BondSchedule
                .GroupBy(s => s.Year)
                .Select(g => new BondSchedulePeriodDto(g.Key, null, g.Sum(s => s.Amount), [.. g.SelectMany(s => s.Bonds)]))
                .OrderBy(p => p.Year)
                .ToArray();

    public async Task InitializeAsync(CancellationToken ct = default)
    {
        if (!IsLoading && ROIC_Series.Count > 0) return;

        IsLoading         = true;
        HistoryError      = null;
        BondScheduleError = null;

        await Task.WhenAll(LoadHistoryAsync(ct), LoadBondScheduleAsync(ct));

        IsLoading = false;
    }

    private async Task LoadHistoryAsync(CancellationToken ct)
    {
        try
        {
            var data = await portfolioService.GetIndexedHistoryAsync(ct);
            ROIC_Series = [.. data.Select(p => new IndexedPoint(p.Date, p.ROIC))];
            LifeStrategySeries = [.. data.Where(p => p.LifeStrategy.HasValue).Select(p => new IndexedPoint(p.Date, p.LifeStrategy!.Value))];
            MsciWorldSeries    = [.. data.Where(p => p.MsciWorld.HasValue).Select(p => new IndexedPoint(p.Date, p.MsciWorld!.Value))];
        }
        catch (Exception ex)
        {
            HistoryError = string.Format(localizationService.Translate("Error_LoadingHistory"), ex.Message);
        }
    }

    private async Task LoadBondScheduleAsync(CancellationToken ct)
    {
        try
        {
            BondSchedule = await portfolioService.GetBondScheduleAsync(ct);
        }
        catch (Exception ex)
        {
            BondScheduleError = string.Format(localizationService.Translate("Error_LoadingBondSchedule"), ex.Message);
        }
    }
}

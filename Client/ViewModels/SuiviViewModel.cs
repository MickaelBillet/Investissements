using InvestissementsDashboard.Client.Model;
using InvestissementsDashboard.Client.Services;
using InvestissementsDashboard.Shared.Models;

namespace InvestissementsDashboard.Client.ViewModels;

public class SuiviViewModel(IPortfolioService portfolioService, ILocalizationService localizationService)
{
    public bool    IsLoading         { get; private set; } = true;
    public string? HistoryError      { get; private set; }
    public string? BondScheduleError { get; private set; }

    public IReadOnlyList<IndexedPoint> ROIC_Series { get; private set; } = [];
    public IReadOnlyList<IndexedPoint> LifeStrategySeries { get; private set; } = [];
    public IReadOnlyList<IndexedPoint> MsciWorldSeries    { get; private set; } = [];
    public IReadOnlyList<BondScheduleDto> BondSchedule     { get; private set; } = [];

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

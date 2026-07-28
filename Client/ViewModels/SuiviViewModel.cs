using InvestissementsDashboard.Client.Model;
using InvestissementsDashboard.Client.Services;
using InvestissementsDashboard.Shared.Models;

namespace InvestissementsDashboard.Client.ViewModels;

public class SuiviViewModel(IPortfolioService portfolioService, ILocalizationService localizationService)
{
    public bool    IsLoading    { get; private set; } = true;
    public string? ErrorMessage { get; private set; }

    public IReadOnlyList<IndexedPoint> ROIC_Series { get; private set; } = [];
    public IReadOnlyList<IndexedPoint> LifeStrategySeries { get; private set; } = [];
    public IReadOnlyList<IndexedPoint> MsciWorldSeries    { get; private set; } = [];
    public IReadOnlyList<BondScheduleDto> BondSchedule     { get; private set; } = [];

    public async Task InitializeAsync(CancellationToken ct = default)
    {
        if (!IsLoading && ROIC_Series.Count > 0) return;

        IsLoading    = true;
        ErrorMessage = null;
        try
        {
            var data = await portfolioService.GetIndexedHistoryAsync(ct);
            ROIC_Series = [.. data.Select(p => new IndexedPoint(p.Date, p.ROIC))];
            LifeStrategySeries = [.. data.Where(p => p.LifeStrategy.HasValue).Select(p => new IndexedPoint(p.Date, p.LifeStrategy!.Value))];
            MsciWorldSeries    = [.. data.Where(p => p.MsciWorld.HasValue).Select(p => new IndexedPoint(p.Date, p.MsciWorld!.Value))];
            BondSchedule       = await portfolioService.GetBondScheduleAsync(ct);
        }
        catch (Exception ex)
        {
            ErrorMessage = string.Format(localizationService.Translate("Error_LoadingHistory"), ex.Message);
        }
        finally
        {
            IsLoading = false;
        }
    }
}

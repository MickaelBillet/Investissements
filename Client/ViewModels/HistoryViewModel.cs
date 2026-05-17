using InvestissementsDashboard.Client.Model;
using InvestissementsDashboard.Client.Services;

namespace InvestissementsDashboard.Client.ViewModels;

public class HistoryViewModel(IPortfolioService portfolioService)
{
    public bool    IsLoading    { get; private set; } = true;
    public string? ErrorMessage { get; private set; }

    public IReadOnlyList<IndexedPoint> PortfolioSeries    { get; private set; } = [];
    public IReadOnlyList<IndexedPoint> LifeStrategySeries { get; private set; } = [];
    public IReadOnlyList<IndexedPoint> MsciWorldSeries    { get; private set; } = [];

    public async Task InitializeAsync(CancellationToken ct = default)
    {
        if (!IsLoading && PortfolioSeries.Count > 0) return;

        IsLoading    = true;
        ErrorMessage = null;
        try
        {
            var data = await portfolioService.GetIndexedHistoryAsync(ct);
            PortfolioSeries    = [.. data.Select(p => new IndexedPoint(p.Date, p.Portfolio))];
            LifeStrategySeries = [.. data.Where(p => p.LifeStrategy60.HasValue).Select(p => new IndexedPoint(p.Date, p.LifeStrategy60!.Value))];
            MsciWorldSeries    = [.. data.Where(p => p.MsciWorld.HasValue).Select(p => new IndexedPoint(p.Date, p.MsciWorld!.Value))];
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Impossible de charger l'historique : {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }
}

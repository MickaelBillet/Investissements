using InvestissementsDashboard.Client.Model;
using InvestissementsDashboard.Client.Services;
using InvestissementsDashboard.Shared.Models;

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
            var history = await portfolioService.GetHistoryAsync(ct);
            IndexSeries(history);
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

    private void IndexSeries(IReadOnlyList<SnapshotDto> history)
    {
        var complete = history
            .Where(s => s.PortfolioTotal > 0 && s.LifeStrategy60.HasValue && s.MsciWorld.HasValue)
            .OrderBy(s => s.Date)
            .ToList();

        if (complete.Count == 0) return;

        var t0 = complete[0];
        PortfolioSeries    = [.. complete.Select(s => new IndexedPoint(s.Date, s.PortfolioTotal / t0.PortfolioTotal * 100m))];
        LifeStrategySeries = [.. complete.Select(s => new IndexedPoint(s.Date, s.LifeStrategy60!.Value / t0.LifeStrategy60!.Value * 100m))];
        MsciWorldSeries    = [.. complete.Select(s => new IndexedPoint(s.Date, s.MsciWorld!.Value / t0.MsciWorld!.Value * 100m))];
    }
}

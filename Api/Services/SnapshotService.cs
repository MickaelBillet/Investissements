using InvestissementsDashboard.Shared.Models;
using Microsoft.Extensions.Logging;

namespace InvestissementsDashboard.Api.Services;

internal sealed class SnapshotService : ISnapshotService
{
    private readonly IAppsScriptService _appsScript;
    private readonly ILogger<SnapshotService> _logger;

    public SnapshotService(IAppsScriptService appsScript, ILogger<SnapshotService> logger)
    {
        _appsScript = appsScript;
        _logger     = logger;
    }

    public async Task<SnapshotDto?> GetLastAsync(CancellationToken ct = default)
    {
        return await _appsScript.CallAsync<SnapshotDto>("Snapshot", "getLast", ct);
    }

    public async Task<IReadOnlyList<SnapshotDto>> GetHistoryAsync(CancellationToken ct = default)
    {
        var result = await _appsScript.CallAsync<IReadOnlyList<SnapshotDto>>("Snapshot", "getHistory", ct);
        return result ?? [];
    }
}

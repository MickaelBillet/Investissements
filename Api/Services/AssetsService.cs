using InvestissementsDashboard.Shared.Models;
using Microsoft.Extensions.Logging;

namespace InvestissementsDashboard.Api.Services;

internal sealed class AssetsService : IAssetsService
{
    private static readonly Dictionary<string, string> DimensionServices = new(StringComparer.OrdinalIgnoreCase)
    {
        ["assetClass"]  = "AssetClass",
        ["supportType"] = "SupportType",
        ["support"]     = "Support",
        ["assetType"]   = "AssetType",
    };

    private readonly IAppsScriptService _appsScript;
    private readonly ILogger<AssetsService> _logger;

    public AssetsService(IAppsScriptService appsScript, ILogger<AssetsService> logger)
    {
        _appsScript = appsScript;
        _logger     = logger;
    }

    public async Task<IReadOnlyList<AssetDto>> GetAllAsync(CancellationToken ct = default)
    {
        var result = await _appsScript.CallAsync<IReadOnlyList<AssetDto>>("Asset", "getAll", ct);

        if (result is null || result.Count == 0)
            _logger.LogWarning("Apps Script returned no assets.");

        return result ?? [];
    }

    public async Task<IReadOnlyList<DistributionDto>> GetDistributionByDimensionAsync(string dimension, CancellationToken ct = default)
    {
        if (!DimensionServices.TryGetValue(dimension, out var service))
            throw new ArgumentException(
                $"Unknown dimension '{dimension}'. Valid values: {string.Join(", ", DimensionServices.Keys)}.",
                nameof(dimension));

        var result = await _appsScript.CallAsync<IReadOnlyList<DistributionDto>>(service, "getDistribution", ct);
        return result ?? [];
    }

    public async Task<IReadOnlyList<AggregateDto>> GetEtfStocksByInformationAsync(CancellationToken ct = default)
    {
        var result = await _appsScript.CallAsync<IReadOnlyList<AggregateDto>>("AssetType", "getEtfStocksByInformation", null, ct);
        return result ?? [];
    }

    public async Task<IReadOnlyList<AssetDto>> GetByAssetTypeAndInformationAsync(string assetType, string information, CancellationToken ct = default)
    {
        var extra = new Dictionary<string, string> { ["assetType"] = assetType, ["information"] = information };
        var result = await _appsScript.CallAsync<IReadOnlyList<AssetDto>>("AssetType", "getByAssetTypeAndInformation", extra, ct);
        return result ?? [];
    }
}

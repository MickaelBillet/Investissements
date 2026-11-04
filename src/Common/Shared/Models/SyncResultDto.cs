namespace InvestissementsDashboard.Shared.Models;

public record SyncResultDto(
    bool Success,
    int AddedCount,
    string? ErrorMessage
);

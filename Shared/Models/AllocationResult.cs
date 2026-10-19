namespace InvestissementsDashboard.Shared.Models;

public record AllocationResult(DateOnly Date, decimal TotalValue, IReadOnlyList<AllocationLine> Lines);
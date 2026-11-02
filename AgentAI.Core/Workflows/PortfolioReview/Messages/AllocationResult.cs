using InvestissementsDashboard.Shared.Models;

namespace AgentAI;

/// <param name="Watchlist">The stocks followed but not held: not part of <paramref name="Lines"/> nor of <paramref name="TotalValue"/>.</param>
public record AllocationResult(DateOnly Date, decimal TotalValue, IReadOnlyList<AllocationLine> Lines, IReadOnlyList<WatchlistItemDto> Watchlist);
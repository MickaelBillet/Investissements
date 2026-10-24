namespace AgentAI;

public record AllocationResult(DateOnly Date, decimal TotalValue, IReadOnlyList<AllocationLine> Lines);
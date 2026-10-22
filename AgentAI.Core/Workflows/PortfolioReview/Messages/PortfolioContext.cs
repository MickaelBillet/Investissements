namespace AgentAI;

/// <summary>Market and portfolio context gathered alongside the allocation; only the requested date for now.</summary>
public record PortfolioContext(DateOnly Date);

namespace AgentAI;

public record AllocationLine(
    string Name,
    string? Isin,
    string AssetClass,
    string Sector,
    string Region,
    decimal Value,
    decimal Weight,
    // Nullable like their AssetDto source: "ND" purchases make them unknown, which must not read as zero.
    decimal? UnrealizedGain,
    decimal? Roi);
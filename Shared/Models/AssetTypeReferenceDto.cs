namespace InvestissementsDashboard.Shared.Models;

public record AssetTypeReferenceDto(
    int? Id,
    string Name,
    string? LabelFr,
    bool GeoSectorEligible
);

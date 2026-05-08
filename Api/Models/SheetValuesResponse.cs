using System.Text.Json;
using System.Text.Json.Serialization;

namespace InvestissementsDashboard.Api.Models;

internal record SheetValuesResponse(
    [property: JsonPropertyName("values")]
    IReadOnlyList<IReadOnlyList<JsonElement>>? Values
);

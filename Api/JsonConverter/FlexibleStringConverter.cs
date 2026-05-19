using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace InvestissementsDashboard.Api.Json;

// Google Sheets can return any field as a number instead of a string.
internal sealed class FlexibleStringConverter : JsonConverter<string>
{
    public override string? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        => reader.TokenType switch
        {
            JsonTokenType.String => reader.GetString(),
            JsonTokenType.Number => reader.TryGetInt64(out var l)
                                    ? l.ToString()
                                    : reader.GetDouble().ToString(CultureInfo.InvariantCulture),
            JsonTokenType.True   => "true",
            JsonTokenType.False  => "false",
            JsonTokenType.Null   => null,
            _                   => null
        };

    public override void Write(Utf8JsonWriter writer, string value, JsonSerializerOptions options)
        => writer.WriteStringValue(value);
}

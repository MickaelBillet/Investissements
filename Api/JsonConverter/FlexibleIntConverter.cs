using System.Text.Json;
using System.Text.Json.Serialization;

namespace InvestissementsDashboard.Api.Json;

// Google Sheets can return numeric IDs as strings or floats (e.g. "5.0").
internal sealed class FlexibleIntConverter : JsonConverter<int>
{
    public override int Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.Null) return 0;
        if (reader.TokenType == JsonTokenType.Number)
        {
            if (reader.TryGetInt32(out var i)) return i;
            if (reader.TryGetDouble(out var d)) return (int)d;
        }
        if (reader.TokenType == JsonTokenType.String)
        {
            var s = reader.GetString();
            if (double.TryParse(s, System.Globalization.NumberStyles.Any,
                    System.Globalization.CultureInfo.InvariantCulture, out var d))
                return (int)d;
        }
        throw new JsonException($"Cannot convert token '{reader.TokenType}' to Int32.");
    }

    public override void Write(Utf8JsonWriter writer, int value, JsonSerializerOptions options)
        => writer.WriteNumberValue(value);
}

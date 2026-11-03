using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace AnFood.API.JsonConverters;

/// <summary>
/// Parses incoming ISO-8601 date strings without applying implicit timezone conversions.
/// - If the input has 'Z' or an offset, we parse as DateTimeOffset and take its wall-clock DateTime (Kind=Unspecified).
/// - If the input has no offset, we parse as DateTime and force Kind=Unspecified.
/// This avoids the common "+7 hours" shift when clients send UTC timestamps but the server stores local wall-clock times.
/// </summary>
public sealed class CustomDateTimeJsonConverter : JsonConverter<DateTime>
{
    public override DateTime Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType != JsonTokenType.String)
            throw new JsonException($"Expected string for {nameof(DateTime)} but got {reader.TokenType}.");

        var input = reader.GetString();
        if (string.IsNullOrWhiteSpace(input))
            return default;

        // Prefer DateTimeOffset to correctly handle inputs containing 'Z' or an explicit offset.
        if (DateTimeOffset.TryParse(
                input,
                CultureInfo.InvariantCulture,
                DateTimeStyles.RoundtripKind,
                out var dto))
        {
            // dto.DateTime returns a DateTime with Kind=Unspecified representing the same wall-clock time in the provided offset.
            return DateTime.SpecifyKind(dto.DateTime, DateTimeKind.Unspecified);
        }

        if (DateTime.TryParse(
                input,
                CultureInfo.InvariantCulture,
                DateTimeStyles.RoundtripKind,
                out var dt))
        {
            return DateTime.SpecifyKind(dt, DateTimeKind.Unspecified);
        }

        throw new JsonException($"Invalid {nameof(DateTime)} value: '{input}'.");
    }

    public override void Write(Utf8JsonWriter writer, DateTime value, JsonSerializerOptions options)
    {
        // Use round-trip format; do not introduce implicit timezone conversion here.
        writer.WriteStringValue(value.ToString("O", CultureInfo.InvariantCulture));
    }
}

public sealed class NullableCustomDateTimeJsonConverter : JsonConverter<DateTime?>
{
    private readonly CustomDateTimeJsonConverter inner = new();

    public override DateTime? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.Null)
            return null;

        return inner.Read(ref reader, typeof(DateTime), options);
    }

    public override void Write(Utf8JsonWriter writer, DateTime? value, JsonSerializerOptions options)
    {
        if (value is null)
        {
            writer.WriteNullValue();
            return;
        }

        inner.Write(writer, value.Value, options);
    }
}


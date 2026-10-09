using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace InvoiceXml.Zugferd.XRechnung.Internal;

/// <summary>Serializer settings matching the API's JSON wire format.</summary>
internal static class InvoiceXmlJson
{
    public static JsonSerializerOptions Options { get; } = Create();

    private static JsonSerializerOptions Create()
    {
        var options = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DictionaryKeyPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            PropertyNameCaseInsensitive = true,
            NumberHandling = JsonNumberHandling.AllowReadingFromString,
        };
        options.Converters.Add(new DateConverter());
        options.Converters.Add(new EnumWireConverterFactory());
        options.Converters.Add(new InvoiceNoteConverter());
        return options;
    }
}

/// <summary>Dates travel as <c>yyyy-MM-dd</c> without a time part.</summary>
internal sealed class DateConverter : JsonConverter<DateTime>
{
    private const string Format = "yyyy-MM-dd";

    public override DateTime Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var text = reader.GetString();
        if (string.IsNullOrWhiteSpace(text))
            throw new JsonException("Expected a date.");

        if (DateTime.TryParseExact(text, Format, CultureInfo.InvariantCulture, DateTimeStyles.None, out var date))
            return date;
        if (DateTime.TryParse(text, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out date))
            return date.Date;

        throw new JsonException($"'{text}' is not a valid date.");
    }

    public override void Write(Utf8JsonWriter writer, DateTime value, JsonSerializerOptions options) =>
        writer.WriteStringValue(value.ToString(Format, CultureInfo.InvariantCulture));
}

/// <summary>
/// Writes enums in the API's wire form (names, numeric codes for <see cref="InvoiceTypeCode"/>,
/// lower case for language and syntax) and reads them case-insensitively. An unknown value read
/// into a nullable enum becomes null, so a code added to the API later never breaks parsing.
/// </summary>
internal sealed class EnumWireConverterFactory : JsonConverterFactory
{
    public override bool CanConvert(Type typeToConvert) =>
        typeToConvert.IsEnum || Nullable.GetUnderlyingType(typeToConvert)?.IsEnum == true;

    public override JsonConverter CreateConverter(Type typeToConvert, JsonSerializerOptions options)
    {
        var underlying = Nullable.GetUnderlyingType(typeToConvert);
        var converterType = underlying is null
            ? typeof(EnumWireConverter<>).MakeGenericType(typeToConvert)
            : typeof(NullableEnumWireConverter<>).MakeGenericType(underlying);
        return (JsonConverter)Activator.CreateInstance(converterType)!;
    }
}

internal static class EnumWire<T>
    where T : struct, Enum
{
    private static readonly Dictionary<string, T> ByText = Build();

    public static string ToWire(T value)
    {
        if (typeof(T) == typeof(InvoiceTypeCode))
            return Convert.ToInt32(value, CultureInfo.InvariantCulture).ToString(CultureInfo.InvariantCulture);

        var name = value.ToString();
        return typeof(T) == typeof(PdfLanguage) || typeof(T) == typeof(XRechnungSyntax)
            ? name.ToLowerInvariant()
            : name;
    }

    public static bool TryRead(ref Utf8JsonReader reader, out T value)
    {
        value = default;
        switch (reader.TokenType)
        {
            case JsonTokenType.String:
                var text = reader.GetString();
                return text is not null && ByText.TryGetValue(text.Trim(), out value);
            case JsonTokenType.Number when reader.TryGetInt32(out var number):
                return ByText.TryGetValue(number.ToString(CultureInfo.InvariantCulture), out value);
            default:
                return false;
        }
    }

    private static Dictionary<string, T> Build()
    {
        var map = new Dictionary<string, T>(StringComparer.OrdinalIgnoreCase);
        foreach (var value in Enum.GetValues(typeof(T)).Cast<T>())
        {
            map[value.ToString()] = value;
            map[Convert.ToInt64(value, CultureInfo.InvariantCulture).ToString(CultureInfo.InvariantCulture)] = value;
            map[ToWire(value)] = value;
        }

        return map;
    }
}

internal sealed class EnumWireConverter<T> : JsonConverter<T>
    where T : struct, Enum
{
    public override T Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) =>
        EnumWire<T>.TryRead(ref reader, out var value)
            ? value
            : throw new JsonException($"Unknown {typeof(T).Name} value.");

    public override void Write(Utf8JsonWriter writer, T value, JsonSerializerOptions options) =>
        writer.WriteStringValue(EnumWire<T>.ToWire(value));
}

internal sealed class NullableEnumWireConverter<T> : JsonConverter<T?>
    where T : struct, Enum
{
    public override T? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) =>
        EnumWire<T>.TryRead(ref reader, out var value) ? value : null;

    public override void Write(Utf8JsonWriter writer, T? value, JsonSerializerOptions options)
    {
        if (value.HasValue)
            writer.WriteStringValue(EnumWire<T>.ToWire(value.Value));
        else
            writer.WriteNullValue();
    }
}

/// <summary>A note travels as a plain string, or as an object when it carries a subject code.</summary>
internal sealed class InvoiceNoteConverter : JsonConverter<InvoiceNote>
{
    public override InvoiceNote? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        switch (reader.TokenType)
        {
            case JsonTokenType.Null:
                return null;
            case JsonTokenType.String:
                return new InvoiceNote { Note = reader.GetString() };
            case JsonTokenType.StartObject:
                var note = new InvoiceNote();
                while (reader.Read() && reader.TokenType != JsonTokenType.EndObject)
                {
                    var name = reader.GetString();
                    reader.Read();
                    var text = reader.TokenType == JsonTokenType.String ? reader.GetString() : null;
                    if (string.Equals(name, "note", StringComparison.OrdinalIgnoreCase))
                        note.Note = text;
                    else if (string.Equals(name, "subjectCode", StringComparison.OrdinalIgnoreCase))
                        note.SubjectCode = text;
                    else
                        reader.Skip();
                }

                return note;
            default:
                throw new JsonException("Expected a note string or object.");
        }
    }

    public override void Write(Utf8JsonWriter writer, InvoiceNote value, JsonSerializerOptions options)
    {
        if (string.IsNullOrEmpty(value.SubjectCode))
        {
            writer.WriteStringValue(value.Note ?? string.Empty);
            return;
        }

        writer.WriteStartObject();
        writer.WriteString("subjectCode", value.SubjectCode);
        writer.WriteString("note", value.Note ?? string.Empty);
        writer.WriteEndObject();
    }
}

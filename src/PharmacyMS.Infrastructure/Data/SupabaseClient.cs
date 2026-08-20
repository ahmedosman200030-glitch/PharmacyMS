using System.Globalization;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace PharmacyMS.Infrastructure.Data;

public class IntToBoolConverter : JsonConverter<bool>
{
    public override bool Read(ref Utf8JsonReader reader, Type type, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.Number) return reader.GetInt32() != 0;
        if (reader.TokenType == JsonTokenType.True) return true;
        if (reader.TokenType == JsonTokenType.False) return false;
        return false;
    }
    public override void Write(Utf8JsonWriter writer, bool value, JsonSerializerOptions options)
        => writer.WriteBooleanValue(value);
}

public class FlexibleDateTimeConverter : JsonConverter<DateTime>
{
    private static readonly string[] Formats = [
        "yyyy-MM-dd HH:mm:ss.ffffffzzz",
        "yyyy-MM-dd HH:mm:ss.fffffzzz",
        "yyyy-MM-dd HH:mm:ss.ffffzzz",
        "yyyy-MM-dd HH:mm:ss.fffzzz",
        "yyyy-MM-dd HH:mm:sszzz",
        "yyyy-MM-ddTHH:mm:ss.fffffffK",
        "yyyy-MM-ddTHH:mm:ssK",
        "yyyy-MM-dd HH:mm:ss",
        "o"
    ];

    public override DateTime Read(ref Utf8JsonReader reader, Type type, JsonSerializerOptions options)
    {
        var str = reader.GetString() ?? "";
        foreach (var fmt in Formats)
        {
            if (DateTime.TryParseExact(str, fmt, CultureInfo.InvariantCulture,
                DateTimeStyles.AdjustToUniversal, out var dt))
                return dt;
        }
        if (DateTime.TryParse(str, CultureInfo.InvariantCulture,
            DateTimeStyles.AdjustToUniversal, out var result))
            return result;
        return DateTime.UtcNow;
    }

    public override void Write(Utf8JsonWriter writer, DateTime value, JsonSerializerOptions options)
        => writer.WriteStringValue(value.ToString("o"));
}

public class FlexibleNullableDateTimeConverter : JsonConverter<DateTime?>
{
    private readonly FlexibleDateTimeConverter _inner = new();

    public override DateTime? Read(ref Utf8JsonReader reader, Type type, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.Null) return null;
        return _inner.Read(ref reader, typeof(DateTime), options);
    }

    public override void Write(Utf8JsonWriter writer, DateTime? value, JsonSerializerOptions options)
    {
        if (value == null) writer.WriteNullValue();
        else writer.WriteStringValue(value.Value.ToString("o"));
    }
}

public class SupabaseClient
{
    public HttpClient Http { get; }
    public string BaseUrl { get; }
    public JsonSerializerOptions JsonOptions { get; }

    public SupabaseClient(string supabaseUrl, string anonKey)
    {
        BaseUrl = $"{supabaseUrl}/rest/v1";
        Http = new HttpClient();
        Http.DefaultRequestHeaders.Add("apikey", anonKey);
        Http.DefaultRequestHeaders.Add("Authorization", $"Bearer {anonKey}");
        Http.DefaultRequestHeaders.Accept.Add(
            new MediaTypeWithQualityHeaderValue("application/json"));
        Http.DefaultRequestHeaders.Add("Prefer", "return=representation");

        JsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            Converters =
            {
                new IntToBoolConverter(),
                new FlexibleDateTimeConverter(),
                new FlexibleNullableDateTimeConverter()
            }
        };
    }
}

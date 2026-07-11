using System.Text.Json;
using System.Text.Json.Serialization;

namespace EventDrivenArchitecturePlayground.Infrastructure.Serialization;

public sealed class BrasiliaDateTimeConverter : JsonConverter<DateTime>
{
    private static readonly TimeZoneInfo _brasiliaZone = TimeZoneInfo.FindSystemTimeZoneById("E. South America Standard Time");

    public override DateTime Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        string? str = reader.GetString();

        if (string.IsNullOrEmpty(str))
        {
            return default;
        }

        return DateTime.Parse(str);
    }

    public override void Write(Utf8JsonWriter writer, DateTime value, JsonSerializerOptions options)
    {
        DateTime brasiliaTime = TimeZoneInfo.ConvertTime(value, _brasiliaZone);
        writer.WriteStringValue(brasiliaTime.ToString("yyyy-MM-ddTHH:mm:ss"));
    }
}
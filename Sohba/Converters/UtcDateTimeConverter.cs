using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Sohba.Converters
{
    /// <summary>
    /// Ensures every DateTime serialized to JSON is unambiguously UTC.
    /// EF Core/SQL Server round-trips DateTime as Kind=Unspecified, which
    /// System.Text.Json then serializes without a "Z" suffix. Browsers parsing
    /// such a string via `new Date(...)` interpret it as LOCAL time — the root
    /// cause of newly-created Posts/Stories appearing hours old. All app
    /// timestamps are produced via DateTime.UtcNow, so Unspecified is safely
    /// treated as UTC here.
    /// </summary>
    public class UtcDateTimeConverter : JsonConverter<DateTime>
    {
        public override DateTime Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            var value = reader.GetDateTime();
            return value.Kind == DateTimeKind.Unspecified
                ? DateTime.SpecifyKind(value, DateTimeKind.Utc)
                : value;
        }

        public override void Write(Utf8JsonWriter writer, DateTime value, JsonSerializerOptions options)
        {
            var utcValue = value.Kind == DateTimeKind.Unspecified
                ? DateTime.SpecifyKind(value, DateTimeKind.Utc)
                : value.ToUniversalTime();

            writer.WriteStringValue(utcValue);
        }
    }
}
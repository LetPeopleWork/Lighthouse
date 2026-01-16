using System.Text.Json;
using System.Text.Json.Serialization;

namespace Lighthouse.Backend.API.JsonConverters
{
    /// <summary>
    /// JSON converter that ensures all DateTime values are treated as UTC.
    /// Incoming dates without timezone info are assumed to be UTC.
    /// Outgoing dates are always serialized with UTC timezone indicator.
    /// </summary>
    public class UtcDateTimeConverter : JsonConverter<DateTime>
    {
        public override DateTime Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            var dateTime = reader.GetDateTime();
            
            // If the DateTime kind is Unspecified, treat it as UTC
            if (dateTime.Kind == DateTimeKind.Unspecified)
            {
                return DateTime.SpecifyKind(dateTime, DateTimeKind.Utc);
            }
            
            // Convert to UTC if it's in local time
            return dateTime.ToUniversalTime();
        }

        public override void Write(Utf8JsonWriter writer, DateTime value, JsonSerializerOptions options)
        {
            // Ensure the value is in UTC before serializing
            var utcDateTime = value.Kind == DateTimeKind.Unspecified 
                ? DateTime.SpecifyKind(value, DateTimeKind.Utc) 
                : value.ToUniversalTime();
            
            writer.WriteStringValue(utcDateTime);
        }
    }
}

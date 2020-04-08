using System;
using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace NCoreUtils.Logging.Google.Data
{
    public class LatencyConveter : JsonConverter<TimeSpan?>
    {
        public override TimeSpan? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            throw new NotImplementedException();
        }

        public override void Write(Utf8JsonWriter writer, TimeSpan? value, JsonSerializerOptions options)
        {
            if (value.HasValue)
            {
                var seconds = value.Value.TotalSeconds;
                // TODO: handle extreme cases...
                writer.WriteStringValue(seconds.ToString("G9", CultureInfo.InvariantCulture) + "s");
            }
            else
            {
                writer.WriteNullValue();
            }
        }
    }
}
using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace NCoreUtils.Logging.Google.Data
{
    public class TimestampConverter : JsonConverter<DateTimeOffset>
    {
        private static readonly DateTimeOffset _epoch = new DateTimeOffset(1970, 1, 1, 0, 0, 0, 0, TimeSpan.Zero);

        public override DateTimeOffset Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            throw new NotImplementedException();
        }

        public override void Write(Utf8JsonWriter writer, DateTimeOffset value, JsonSerializerOptions options)
        {
            var diff = value.ToUniversalTime().Subtract(_epoch);
            var mss = diff.Ticks;
            var seconds = mss / TimeSpan.TicksPerSecond;
            var nanos = (mss % TimeSpan.TicksPerSecond) * 100;
            writer.WriteStartObject();
            writer.WriteNumber("seconds", seconds);
            writer.WriteNumber("nanos", nanos);
            writer.WriteEndObject();
        }
    }
}
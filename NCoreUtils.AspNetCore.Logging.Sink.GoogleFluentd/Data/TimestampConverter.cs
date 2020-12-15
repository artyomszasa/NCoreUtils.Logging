using System;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace NCoreUtils.Logging.Google.Data
{
    public class TimestampConverter : JsonConverter<DateTimeOffset>
    {
        public sealed class Proxy
        {
            [JsonPropertyName("seconds")]
            public long Seconds { get; set; }

            [JsonPropertyName("nanos")]
            public long Nanos { get; set; }
        }

        private static readonly DateTimeOffset _epoch;

        [ExcludeFromCodeCoverage]
        static TimestampConverter()
        {
            _epoch = new DateTimeOffset(1970, 1, 1, 0, 0, 0, 0, TimeSpan.Zero);
        }

        public override DateTimeOffset Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            var proxy = JsonSerializer.Deserialize<Proxy>(ref reader, options);
            var ts = new TimeSpan(proxy.Nanos / 100 + proxy.Seconds * TimeSpan.TicksPerSecond);
            return _epoch.Add(ts);
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
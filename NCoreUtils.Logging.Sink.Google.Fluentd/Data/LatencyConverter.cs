using System.Buffers;
using System.Buffers.Text;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace NCoreUtils.Logging.Google.Data;

[ExcludeFromCodeCoverage]
public class LatencyConveter : JsonConverter<TimeSpan?>
{
    private static readonly StandardFormat G9 = StandardFormat.Parse("G9");

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
            Span<byte> buffer = stackalloc byte[32];
            if (Utf8Formatter.TryFormat(seconds, buffer, out var size, G9) && size < buffer.Length)
            {
                buffer[size++] = (byte)'s';
                writer.WriteStringValue(buffer[..size]);
            }
            else
            {
                writer.WriteStringValue(seconds.ToString("G9", CultureInfo.InvariantCulture) + "s");
            }
        }
        else
        {
            writer.WriteNullValue();
        }
    }
}
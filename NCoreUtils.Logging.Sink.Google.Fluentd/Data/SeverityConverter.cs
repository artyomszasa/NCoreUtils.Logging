using System.Buffers;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace NCoreUtils.Logging.Google.Data;

[ExcludeFromCodeCoverage]
public class SeverityConverter : JsonConverter<LogSeverity>
{
    private static LogSeverity ReadString(ReadOnlySpan<byte> data)
    {
        if (MemoryExtensions.SequenceEqual(data, "DEFAULT"u8)) { return LogSeverity.Default; }
        if (MemoryExtensions.SequenceEqual(data, "DEBUG"u8)) { return LogSeverity.Debug; }
        if (MemoryExtensions.SequenceEqual(data, "INFO"u8)) { return LogSeverity.Info; }
        if (MemoryExtensions.SequenceEqual(data, "NOTICE"u8)) { return LogSeverity.Notice; }
        if (MemoryExtensions.SequenceEqual(data, "WARNING"u8)) { return LogSeverity.Warning; }
        if (MemoryExtensions.SequenceEqual(data, "ERROR"u8)) { return LogSeverity.Error; }
        if (MemoryExtensions.SequenceEqual(data, "CRITICAL"u8)) { return LogSeverity.Critical; }
        if (MemoryExtensions.SequenceEqual(data, "ALERT"u8)) { return LogSeverity.Alert; }
        if (MemoryExtensions.SequenceEqual(data, "EMERGENCY"u8)) { return LogSeverity.Emergency; }
        return default;
    }

    private static LogSeverity ReadString(in Utf8JsonReader reader)
    {
        if (reader.HasValueSequence)
        {
            var l = reader.ValueSequence.Length;
            if (l > 12)
            {
                return default;
            }
            Span<byte> buffer = stackalloc byte[unchecked((int)l)];
            reader.ValueSequence.CopyTo(buffer);
            return ReadString(buffer);
        }
        return ReadString(reader.ValueSpan);
    }

    public override LogSeverity Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        return reader.TokenType switch
        {
            JsonTokenType.String => ReadString(in reader),
            var tokenType => throw new InvalidOperationException($"Unable to convert {tokenType} to LogSeverity.")
        };
    }

    public override void Write(Utf8JsonWriter writer, LogSeverity value, JsonSerializerOptions options)
    {
        switch (value)
        {
            case LogSeverity.Default: writer.WriteStringValue("DEFAULT"u8); break;
            case LogSeverity.Debug: writer.WriteStringValue("DEBUG"u8); break;
            case LogSeverity.Info: writer.WriteStringValue("INFO"u8); break;
            case LogSeverity.Notice: writer.WriteStringValue("NOTICE"u8); break;
            case LogSeverity.Warning: writer.WriteStringValue("WARNING"u8); break;
            case LogSeverity.Error: writer.WriteStringValue("ERROR"u8); break;
            case LogSeverity.Critical: writer.WriteStringValue("CRITICAL"u8); break;
            case LogSeverity.Alert: writer.WriteStringValue("ALERT"u8); break;
            case LogSeverity.Emergency: writer.WriteStringValue("EMERGENCY"u8); break;
            default:
                writer.WriteStringValue(((int)value).ToString());
                break;
        }
    }
}
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace NCoreUtils.Logging.Google.Data;

[ExcludeFromCodeCoverage]
public class SeverityConverter : JsonConverter<LogSeverity>
{
    private static readonly IReadOnlyDictionary<LogSeverity, string> _names = new Dictionary<LogSeverity, string>
    {
        { LogSeverity.Default, "DEFAULT" },
        { LogSeverity.Debug, "DEBUG" },
        { LogSeverity.Info, "INFO" },
        { LogSeverity.Notice, "NOTICE" },
        { LogSeverity.Warning, "WARNING" },
        { LogSeverity.Error, "ERROR" },
        { LogSeverity.Critical, "CRITICAL" },
        { LogSeverity.Alert, "ALERT" },
        { LogSeverity.Emergency, "EMERGENCY" }
    };

    private static LogSeverity ReadString(ref Utf8JsonReader reader)
    {
        var value = reader.GetString();
        return _names.Where(kv => kv.Value == value).Select(kv => kv.Key).FirstOrDefault();
    }

    public override LogSeverity Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        return reader.TokenType switch
        {
            JsonTokenType.String => ReadString(ref reader),
            var tokenType => throw new InvalidOperationException($"Unable to convert {tokenType} to LogSeverity.")
        };
    }

    public override void Write(Utf8JsonWriter writer, LogSeverity value, JsonSerializerOptions options)
    {
        writer.WriteStringValue(_names.TryGetValue(value, out var svalue) ? svalue : ((int)value).ToString());
    }
}
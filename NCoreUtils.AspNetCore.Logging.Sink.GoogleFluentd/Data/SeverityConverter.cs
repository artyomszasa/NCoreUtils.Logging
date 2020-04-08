using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace NCoreUtils.Logging.Google.Data
{
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

        public override LogSeverity Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            throw new NotImplementedException();
        }

        public override void Write(Utf8JsonWriter writer, LogSeverity value, JsonSerializerOptions options)
        {
            writer.WriteStringValue(_names.TryGetValue(value, out var svalue) ? svalue : ((int)value).ToString());
        }
    }
}
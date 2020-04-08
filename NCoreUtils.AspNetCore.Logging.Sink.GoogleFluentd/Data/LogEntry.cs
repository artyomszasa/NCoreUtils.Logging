using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace NCoreUtils.Logging.Google.Data
{
    public class LogEntry
    {
        private static readonly IReadOnlyDictionary<string, string> _noLabels = new Dictionary<string, string>();

        public string LogName { get; }

        [JsonConverter(typeof(SeverityConverter))]
        public LogSeverity Severity { get; }

        public string Message { get; }

        [JsonConverter(typeof(TimestampConverter))]
        public DateTimeOffset Timestamp { get; }

        public ServiceContext? ServiceContext { get; }

        public ErrorContext? Context { get; }

        public HttpRequest? HttpRequest { get; }

        [JsonPropertyName("logging.googleapis.com/labels")]
        public IReadOnlyDictionary<string, string> Labels { get; }

        public LogEntry(
            string logName,
            LogSeverity severity,
            string message,
            DateTimeOffset timestamp,
            ServiceContext? serviceContext,
            ErrorContext? context,
            HttpRequest? httpRequest,
            IReadOnlyDictionary<string, string>? labels)
        {
            LogName = logName;
            Severity = severity;
            Message = message;
            Timestamp = timestamp;
            ServiceContext = serviceContext;
            Context = context;
            HttpRequest = httpRequest;
            Labels = labels ?? _noLabels;
        }
    }
}
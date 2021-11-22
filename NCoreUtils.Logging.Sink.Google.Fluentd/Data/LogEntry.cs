using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace NCoreUtils.Logging.Google.Data
{
    public class LogEntry
    {
        private static readonly IReadOnlyDictionary<string, string> _noLabels = new Dictionary<string, string>();

        [JsonPropertyName("logName")]
        public string LogName { get; }

        [JsonConverter(typeof(SeverityConverter))]
        [JsonPropertyName("severity")]
        public LogSeverity Severity { get; }

        [JsonPropertyName("message")]
        public string Message { get; }

        [JsonConverter(typeof(TimestampConverter))]
        [JsonPropertyName("timestamp")]
        public DateTimeOffset Timestamp { get; }

        [JsonPropertyName("serviceContext")]
        public ServiceContext? ServiceContext { get; }

        [JsonPropertyName("context")]
        public ErrorContext? Context { get; }

        [JsonPropertyName("httpRequest")]
        public HttpRequest? HttpRequest { get; }

        [JsonPropertyName("logging.googleapis.com/trace")]
        public string? Trace { get; }

        [JsonPropertyName("logging.googleapis.com/labels")]
        public IReadOnlyDictionary<string, string> Labels { get; }

        [JsonConstructor]
        public LogEntry(
            string logName,
            LogSeverity severity,
            string message,
            DateTimeOffset timestamp,
            ServiceContext? serviceContext,
            ErrorContext? context,
            HttpRequest? httpRequest,
            string? trace,
            IReadOnlyDictionary<string, string>? labels)
        {
            LogName = logName;
            Severity = severity;
            Message = message;
            Timestamp = timestamp;
            ServiceContext = serviceContext;
            Context = context;
            HttpRequest = httpRequest;
            Trace = trace;
            Labels = labels ?? _noLabels;
        }
    }
}
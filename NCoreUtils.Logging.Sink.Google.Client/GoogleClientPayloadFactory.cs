using System;
using System.Collections.Generic;
using Google.Cloud.Logging.Type;
using Google.Cloud.Logging.V2;
using Google.Protobuf.WellKnownTypes;
using Microsoft.Extensions.Logging;

namespace NCoreUtils.Logging.Google
{
    public class GoogleClientPayloadFactory : GooglePayloadFactory<LogEntry, IGoogleClientSinkConfiguration>
    {
        private static readonly WebContext _noWebContext = default;

        protected static LogSeverity GetLogSeverity(LogLevel logLevel)
            => logLevel switch
            {
                LogLevel.Trace => LogSeverity.Debug,
                LogLevel.Debug => LogSeverity.Debug,
                LogLevel.Information => LogSeverity.Info,
                LogLevel.Warning => LogSeverity.Warning,
                LogLevel.Error => LogSeverity.Error,
                LogLevel.Critical => LogSeverity.Critical,
                _ => LogSeverity.Default
            };


        public GoogleClientPayloadFactory(IGoogleClientSinkConfiguration configuration, IEnumerable<ILabelProvider> labelProviders)
            : base(configuration, labelProviders)
        { }

        public override LogEntry CreatePayload<TState>(LogMessage<TState> message)
        {
            if (message is WebLogMessage webMessage)
            {
                return CreateLogEntry(
                    webMessage.Timestamp,
                    webMessage.Category,
                    webMessage.LogLevel,
                    webMessage.EventId,
                    webMessage.Exception,
                    webMessage.State,
                    webMessage.Formatter,
                    webMessage.Context,
                    webMessage.IsRequestSummary
                );
            }
            return CreateLogEntry(
                message.Timestamp,
                message.Category,
                message.LogLevel,
                message.EventId,
                message.Exception,
                message.State,
                message.Formatter,
                _noWebContext,
                false
            );
        }

        protected virtual HttpRequest? CreateHttpRequest(in WebContext context)
        {
            if (string.IsNullOrEmpty(context.Url))
            {
                return default;
            }
            var result = new HttpRequest
            {
                RequestMethod = context.Method ?? string.Empty,
                RequestUrl = context.Url ?? string.Empty,
                Status = context.ResponseStatusCode ?? 0,
                UserAgent = context.UserAgent ?? string.Empty,
                RemoteIp = context.RemoteIp ?? string.Empty,
                Referer = context.Referrer ?? string.Empty
            };
            if (context.Latency.HasValue)
            {
                result.Latency = Duration.FromTimeSpan(context.Latency.Value);
            }
            if (context.ResponseContentLength.HasValue)
            {
                result.ResponseSize = context.ResponseContentLength.Value;
            }
            return result;
        }

        protected virtual Struct CreateJsonPayload(
            DateTimeOffset timestamp,
            string serviceName,
            string? serviceVersion,
            string message,
            in WebContext ctx)
        {
            // Service Context
            var serviceContext = new Struct()
                .Add("service", serviceName)
                .Add("version", serviceVersion);
            // Context
            var context = new Struct()
                .Add("method", ctx.Method)
                .Add("url", ctx.Url)
                .Add("userAgent", ctx.UserAgent)
                .Add("referer", ctx.Referrer)
                .Add("responseStatusCode", ctx.ResponseStatusCode ?? 0)
                .Add("remoteIp", ctx.RemoteIp)
                .Add("user", ctx.User);
            // Payload
            return new Struct()
                .Add("eventTime", timestamp.ToString("o"))
                .Add("serviceContext", serviceContext)
                .Add("message", message)
                .Add("context", context);
        }

        private LogEntry CreateLogEntry<TState>(
            DateTimeOffset timestamp,
            string category,
            LogLevel logLevel,
            EventId eventId,
            Exception? exception,
            TState state,
            Func<TState, Exception?, string> formatter,
            in WebContext context,
            bool isRequestSummary)
        {
            var labels = new Dictionary<string, string>();
            if (Configuration.CategoryHandling == CategoryHandling.IncludeAsLabel)
            {
                labels.Add("category", category);
            }
            foreach (var labelProvider in LabelProviders)
            {
                labelProvider.UpdateLabels(category, eventId, logLevel, in context, labels);
            }
            var logEntry = new LogEntry
            {
                LogName = Configuration.LogName,
                Resource = Configuration.Resource,
                Severity = GetLogSeverity(logLevel),
                Timestamp = global::Google.Protobuf.WellKnownTypes.Timestamp.FromDateTimeOffset(timestamp)
            };
            foreach (var kv in labels)
            {
                logEntry.Labels.Add(kv.Key, kv.Value);
            }
            if (isRequestSummary)
            {
                logEntry.HttpRequest = CreateHttpRequest(context);
            }
            var textPayload = CreateTextPayload(Configuration, eventId, category, formatter(state, exception), exception?.ToString());
            if (exception != null)
            {
                logEntry.JsonPayload = CreateJsonPayload(timestamp, Configuration.Service, Configuration.ServiceVersion, textPayload ?? string.Empty, in context);
            }
            else
            {
                logEntry.TextPayload = textPayload;
            }
            return logEntry;
        }
    }
}
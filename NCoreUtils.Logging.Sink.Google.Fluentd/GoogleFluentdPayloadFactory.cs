using System;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;
using NCoreUtils.Logging.Google.Data;

namespace NCoreUtils.Logging.Google
{
    public class GoogleFluentdPayloadFactory : GooglePayloadFactory<LogEntry, IGoogleFluentdSinkConfiguration>
    {
        private static readonly WebContext _noWebContext = default;

        private static readonly IReadOnlyDictionary<LogLevel, LogSeverity> _level2severity = new Dictionary<LogLevel, LogSeverity>
        {
            { LogLevel.Trace,       LogSeverity.Debug },
            { LogLevel.Debug,       LogSeverity.Debug },
            { LogLevel.Information, LogSeverity.Info },
            { LogLevel.Warning,     LogSeverity.Warning },
            { LogLevel.Error,       LogSeverity.Error },
            { LogLevel.Critical,    LogSeverity.Critical }
        };

        protected static LogSeverity GetLogSeverity(LogLevel logLevel)
            => _level2severity.TryGetValue(logLevel, out var severity) ? severity : LogSeverity.Default;

        public GoogleFluentdPayloadFactory(IGoogleFluentdSinkConfiguration configuration, IEnumerable<ILabelProvider> labelProviders)
            : base(configuration, labelProviders)
        { }

        protected virtual LogEntry CreateLogEntry<TState>(
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
            HttpRequest? request = null;
            ErrorContext? errorContext = null;
            ServiceContext? serviceContext = null;
            if (isRequestSummary)
            {
                request = new HttpRequest(
                    context.Method ?? string.Empty,
                    context.Url ?? string.Empty,
                    context.ResponseStatusCode ?? default,
                    context.ResponseContentLength,
                    context.UserAgent ?? string.Empty,
                    context.RemoteIp ?? string.Empty,
                    context.Referrer ?? string.Empty,
                    context.Latency
                );
            }
            if (exception != null)
            {
                errorContext = new ErrorContext(
                    context.Method ?? string.Empty,
                    context.Url ?? string.Empty,
                    context.UserAgent ?? string.Empty,
                    context.Referrer ?? string.Empty,
                    context.ResponseStatusCode ?? 500,
                    context.RemoteIp ?? string.Empty,
                    context.User ?? string.Empty
                );
                serviceContext = new ServiceContext(
                    Configuration.Service,
                    Configuration.ServiceVersion
                );
            }
            var textPayload = CreateTextPayload(Configuration, eventId, category, formatter(state, exception), exception?.ToString());
            var labels = new Dictionary<string, string>();
            if (Configuration.CategoryHandling == CategoryHandling.IncludeAsLabel)
            {
                labels.Add("category", category);
            }
            foreach (var labelProvider in LabelProviders)
            {
                labelProvider.UpdateLabels(category, eventId, logLevel, in context, labels);
            }
            return new LogEntry(
                logName: Configuration.LogName,
                severity: GetLogSeverity(logLevel),
                message: textPayload,
                timestamp: timestamp,
                serviceContext: serviceContext,
                context: errorContext,
                httpRequest: request,
                trace: Configuration.TraceHandling switch
                {
                    TraceHandling.Enabled => context.TraceId,
                    TraceHandling.Disabled => default,
                    TraceHandling.Summary => request is null ? default : context.TraceId,
                    _ => default
                },
                labels: labels
            );
        }

        public override LogEntry CreatePayload<TState>(LogMessage<TState> message)
        {
            if (message is WebLogMessage<TState> webMessage)
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
    }
}
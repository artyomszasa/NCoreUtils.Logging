using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Microsoft.Extensions.Logging;
using NCoreUtils.Logging.Google.Data;

namespace NCoreUtils.Logging.Google
{
    public class GoogleFluentdPayloadFactory : GooglePayloadFactory<LogEntry, IGoogleFluentdSinkConfiguration>
    {
        private static readonly WebContext _noWebContext = default;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static ref readonly WebContext GetWebContext(LogMessage message, out bool isRequestSummary)
        {
            if (message is WebLogMessage webMessage)
            {
                isRequestSummary = webMessage.IsRequestSummary;
                return ref webMessage.Context;
            }
            isRequestSummary = false;
            return ref _noWebContext;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static HttpRequest CreateOrUpdateHttpRequest(
            string requestMethod,
            string requestUrl,
            int status,
            long? responseSize,
            string userAgent,
            string remoteIp,
            string referer,
            TimeSpan? latency)
            => Pool.HttpRequest.TryRent(out var httpRequest)
                ? httpRequest.Update(requestMethod, requestUrl, status, responseSize, userAgent, remoteIp, referer, latency)
                : new(requestMethod, requestUrl, status, responseSize, userAgent, remoteIp, referer, latency);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static ServiceContext CreateOrUpdateServiceContext(string service, string? version)
            => Pool.ServiceContext.TryRent(out var serviceContext)
                ? serviceContext.Update(service, version)
                : new(service, version);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static ErrorContext CreateOrUpdateErrorContext(
            string method,
            string url,
            string userAgent,
            string referer,
            int responseStatusCode,
            string remoteIp,
            string user)
            => Pool.ErrorContext.TryRent(out var errorContext)
                ? errorContext.Update(method, url, userAgent, referer, responseStatusCode, remoteIp, user)
                : new(method, url, userAgent, referer, responseStatusCode, remoteIp, user);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static LogEntry CreateOrUpdateLogEntry(
            string logName,
            LogSeverity severity,
            string message,
            DateTimeOffset timestamp,
            ServiceContext? serviceContext,
            ErrorContext? context,
            HttpRequest? httpRequest,
            string? trace,
            IReadOnlyDictionary<string, string>? labels)
            => Pool.LogEntry.TryRent(out var logEntry)
                ? logEntry.Update(logName, severity, message, timestamp, serviceContext, context, httpRequest, trace, labels)
                : new(logName, severity, message, timestamp, serviceContext, context, httpRequest, trace, labels);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected static LogSeverity GetLogSeverity(LogLevel logLevel) => logLevel switch
        {
            LogLevel.Trace => LogSeverity.Debug,
            LogLevel.Debug => LogSeverity.Debug,
            LogLevel.Information => LogSeverity.Info,
            LogLevel.Warning => LogSeverity.Warning,
            LogLevel.Error => LogSeverity.Error,
            LogLevel.Critical => LogSeverity.Critical,
            _ => LogSeverity.Default
        };

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
                request = CreateOrUpdateHttpRequest(
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
                errorContext = CreateOrUpdateErrorContext(
                    context.Method ?? string.Empty,
                    context.Url ?? string.Empty,
                    context.UserAgent ?? string.Empty,
                    context.Referrer ?? string.Empty,
                    context.ResponseStatusCode ?? 500,
                    context.RemoteIp ?? string.Empty,
                    context.User ?? string.Empty
                );
                serviceContext = CreateOrUpdateServiceContext(
                    Configuration.Service,
                    Configuration.ServiceVersion
                );
            }
            var textPayload = CreateTextPayload(Configuration, eventId, category, formatter(state, exception), exception?.ToString());
            var labels = Pool.Labels.TryRent(out var labelDictionary) ? labelDictionary : new();
            if (Configuration.CategoryHandling == CategoryHandling.IncludeAsLabel)
            {
                labels.Add("category", category);
            }
            foreach (var labelProvider in LabelProviders)
            {
                labelProvider.UpdateLabels(category, eventId, logLevel, in context, labels);
            }
            return CreateOrUpdateLogEntry(
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
            ref readonly WebContext context = ref GetWebContext(message, out var isRequestSummary);
            return CreateLogEntry(
                message.Timestamp,
                message.Category,
                message.LogLevel,
                message.EventId,
                message.Exception,
                message.State,
                message.Formatter,
                in context,
                isRequestSummary
            );
        }
    }
}
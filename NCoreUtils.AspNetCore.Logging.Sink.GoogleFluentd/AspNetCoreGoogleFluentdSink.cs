using System;
using System.Buffers;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NCoreUtils.AspNetCore;
using NCoreUtils.Logging.Google.Data;
using NCoreUtils.Logging.Google.Fluentd;

namespace NCoreUtils.Logging.Google
{
    public class AspNetCoreGoogleFluentdSink : IAspNetCoreSink
    {
        private static readonly IReadOnlyDictionary<LogLevel, LogSeverity> _level2severity = new Dictionary<LogLevel, LogSeverity>
        {
            { LogLevel.Trace,       LogSeverity.Debug },
            { LogLevel.Debug,       LogSeverity.Debug },
            { LogLevel.Information, LogSeverity.Info },
            { LogLevel.Warning,     LogSeverity.Warning },
            { LogLevel.Error,       LogSeverity.Error },
            { LogLevel.Critical,    LogSeverity.Critical }
        };

        private static readonly JsonSerializerOptions _defaultJsonSerializerOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            IgnoreNullValues = true
        };

        protected static LogSeverity GetLogSeverity(LogLevel logLevel)
            => _level2severity.TryGetValue(logLevel, out var severity) ? severity : LogSeverity.Default;

        private Func<JsonSerializerOptions> _jsonSerializerOptionsSource;

        protected IEnumerable<ILabelProvider> LabelProviders { get; }

        protected AspNetCoreGoogleFluentdLoggingContext Context { get; }

        protected IFluentdSink Sink { get; }

        protected JsonSerializerOptions JsonSerializerOptions => _jsonSerializerOptionsSource();

        public AspNetCoreGoogleFluentdSink(
            AspNetCoreGoogleFluentdLoggingContext context,
            IOptionsMonitor<JsonSerializerOptions>? jsonSerializerOptions = default,
            IEnumerable<ILabelProvider>? labelProviders = null)
        {
            Context = context ?? throw new ArgumentNullException(nameof(context));
            Sink = FluentdSink.Create(new Uri(context.FluentdUri, UriKind.Absolute));
            // JsonSerializerOptions = jsonSerializerOptions ?? _defaultJsonSerializerOptions;
            _jsonSerializerOptionsSource = jsonSerializerOptions is null
                ? new Func<JsonSerializerOptions>(() => _defaultJsonSerializerOptions)
                : new Func<JsonSerializerOptions>(() => jsonSerializerOptions.CurrentValue);
            LabelProviders = labelProviders ?? Enumerable.Empty<ILabelProvider>();
        }

        public ValueTask DisposeAsync()
            => default;

        protected virtual LogEntry CreateLogEntry<TState>(
            DateTimeOffset timestamp,
            string category,
            LogLevel logLevel,
            EventId eventId,
            Exception? exception,
            TState state,
            Func<TState, Exception?, string> formatter,
            AspNetCoreContext context,
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
                    Context.LogId,
                    Context.ServiceVersion
                );
            }
            using var buffer = MemoryPool<char>.Shared.Rent(64 * 1024);
            var textPayload = CreateTextPayload(buffer.Memory.Span, eventId, category, formatter(state, exception), exception?.ToString());
            var labels = new Dictionary<string, string>();
            if (Context.CategoryHandling == CategoryHandling.IncludeAsLabel)
            {
                labels.Add("category", category);
            }
            foreach (var labelProvider in LabelProviders)
            {
                labelProvider.UpdateLabels(category, eventId, logLevel, in context, labels);
            }
            return new LogEntry(
                logName: $"projects/{Context.ProjectId}/logs/{Context.LogId}",
                severity: GetLogSeverity(logLevel),
                message: textPayload,
                timestamp: timestamp,
                serviceContext: serviceContext,
                context: errorContext,
                httpRequest: request,
                labels: labels
            );
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        protected string CreateTextPayload(Span<char> buffer, EventId eventId, string categoryName, string message, string? exception)
        {
            var builder = new SpanBuilder(buffer);
            if (Context.EventIdHandling == EventIdHandling.IncludeAlways || (Context.EventIdHandling == EventIdHandling.IncludeValidIds && eventId.Id != -1 && eventId != 0))
            {
                builder.Append('[');
                builder.Append(eventId.Id);
                builder.Append("] ");
            }
            if (Context.CategoryHandling == CategoryHandling.IncludeInMessage)
            {
                builder.Append('[');
                builder.Append(categoryName);
                builder.Append("] ");
            }
            builder.Append(message);
            if (!string.IsNullOrEmpty(exception))
            {
                builder.Append("\n");
                #if NETSTANDARD2_1
                builder.Append(exception);
                #else
                builder.Append(exception!);
                #endif
            }
            return builder.ToString();
        }

        protected virtual ValueTask LogAsync<TState>(
            DateTimeOffset timestamp,
            string category,
            LogLevel logLevel,
            EventId eventId,
            Exception? exception,
            TState state,
            Func<TState, Exception?, string> formatter,
            AspNetCoreContext context,
            bool isRequestSummary,
            CancellationToken cancellationToken)
        {
            var entry = CreateLogEntry(timestamp, category, logLevel, eventId, exception, state, formatter, context, isRequestSummary);
            var json = JsonSerializer.Serialize(entry, JsonSerializerOptions);
            return Sink.WriteAsync(json, cancellationToken);
        }

        public ValueTask LogAsync<TState>(AspNetCoreLogMessage<TState> message, CancellationToken cancellationToken = default)
            => LogAsync(
                message.Timestamp,
                message.Category,
                message.LogLevel,
                message.EventId,
                message.Exception,
                message.State,
                message.Formatter,
                message.Context,
                message.IsRequestSummary,
                cancellationToken
            );

        public ValueTask LogAsync<TState>(LogMessage<TState> message, CancellationToken cancellationToken = default)
        {
            var aspMessage = message as AspNetCoreLogMessage<TState>;
            return LogAsync(
                message.Timestamp,
                message.Category,
                message.LogLevel,
                message.EventId,
                message.Exception,
                message.State,
                message.Formatter,
                aspMessage?.Context ?? default,
                aspMessage?.IsRequestSummary ?? false,
                cancellationToken
            );
        }
    }
}
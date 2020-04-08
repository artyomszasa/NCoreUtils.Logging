using System;
using System.Buffers;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Google.Cloud.Logging.Type;
using Google.Cloud.Logging.V2;
using Google.Protobuf.WellKnownTypes;
using Microsoft.Extensions.Logging;
using NCoreUtils.AspNetCore;

namespace NCoreUtils.Logging.Google
{
    public class AspNetCoreGoogleSink : GoogleSinkBase, IAspNetCoreBulkSink
    {
        private readonly IEnumerable<ILabelProvider> _labelProviders;

        private readonly AspNetCoreGoogleLoggingContext _context;

        protected override bool IncludeCategory => _context.CategoryHandling == CategoryHandling.IncludeInMessage;

        ISinkQueue IBulkSink.CreateQueue()
            => CreateQueue();

        IAspNetCoreSinkQueue IAspNetCoreBulkSink.CreateQueue()
            => CreateQueue();

        public AspNetCoreGoogleSink(AspNetCoreGoogleLoggingContext context, IEnumerable<ILabelProvider>? labelProviders = null)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _labelProviders = labelProviders ?? Enumerable.Empty<ILabelProvider>();
        }

        private HttpRequest? CreateHttpRequest(in AspNetCoreContext context)
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

        private Struct CreateJsonPayload(
            DateTimeOffset timestamp,
            string serviceName,
            string? serviceVersion,
            string message,
            in AspNetCoreContext ctx)
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
            in AspNetCoreContext context,
            bool isRequestSummary)
        {
            var labels = new Dictionary<string, string>();
            if (_context.CategoryHandling == CategoryHandling.IncludeAsLabel)
            {
                labels.Add("category", category);
            }
            foreach (var labelProvider in _labelProviders)
            {
                labelProvider.UpdateLabels(category, eventId, logLevel, in context, labels);
            }
            var logEntry = new LogEntry
            {
                LogName = _context.LogName.ToString(),
                Resource = _context.Resource,
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
            using var buffer = MemoryPool<char>.Shared.Rent(64 * 1024);
            var textPayload = CreateTextPayload(buffer.Memory.Span, eventId, category, formatter(state, exception), exception?.ToString());
            if (exception != null)
            {
                logEntry.JsonPayload = CreateJsonPayload(timestamp, _context.LogName.LogId, _context.ServiceVersion, textPayload ?? string.Empty, in context);
            }
            else
            {
                logEntry.TextPayload = textPayload;
            }
            return logEntry;
        }

        internal LogEntry CreateLogEntry<TState>(LogMessage<TState> message)
        {
            var aspMessage = message as AspNetCoreLogMessage<TState>;
            return CreateLogEntry(
                message.Timestamp,
                message.Category,
                message.LogLevel,
                message.EventId,
                message.Exception,
                message.State,
                message.Formatter,
                aspMessage?.Context ?? default,
                aspMessage?.IsRequestSummary ?? false
            );
        }

        internal LogEntry CreateLogEntry<TState>(AspNetCoreLogMessage<TState> message)
            => CreateLogEntry(
                message.Timestamp,
                message.Category,
                message.LogLevel,
                message.EventId,
                message.Exception,
                message.State,
                message.Formatter,
                message.Context,
                message.IsRequestSummary
            );

        internal async ValueTask SendAsync(LogEntry[] entries, CancellationToken cancellationToken)
        {
            try
            {
                var client = await GetClientAsync(cancellationToken);
                await client.WriteLogEntriesAsync(_context.LogName, _context.Resource, null, entries, cancellationToken);
            }
            catch (Exception exn) when (TryAsRcpException(exn, out var rpcExn))
            {
                Console.Error.WriteLine($"Unable to write log entries: {rpcExn.Message}.");
                Console.Error.WriteLine(rpcExn);
            }
        }

        protected override bool IncludeEventId(EventId eventId)
            => _context.EventIdHandling == EventIdHandling.IncludeAlways || (_context.EventIdHandling == EventIdHandling.IncludeValidIds && eventId.Id != -1 && eventId != 0);

        public AspNetCoreGoogleSinkQueue CreateQueue()
            => new AspNetCoreGoogleSinkQueue(this);

        public ValueTask LogAsync<TState>(LogMessage<TState> message, CancellationToken cancellationToken = default)
            => SendAsync(new [] { CreateLogEntry(message) }, cancellationToken);

        public ValueTask LogAsync<TState>(AspNetCoreLogMessage<TState> message, CancellationToken cancellationToken = default)
            => SendAsync(new [] { CreateLogEntry(message) }, cancellationToken);

        public ValueTask DisposeAsync() => default;
    }
}
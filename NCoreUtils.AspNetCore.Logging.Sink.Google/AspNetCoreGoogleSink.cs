using System;
using System.Buffers;
using System.Threading;
using System.Threading.Tasks;
using Google.Cloud.Logging.Type;
using Google.Cloud.Logging.V2;
using Google.Protobuf.WellKnownTypes;
using Microsoft.Extensions.Logging;

namespace NCoreUtils.Logging.Google
{
    public class AspNetCoreGoogleSink : GoogleSinkBase, IAspNetCoreBulkSink
    {
        private readonly AspNetCoreGoogleLoggingContext _context;

        ISinkQueue IBulkSink.CreateQueue()
            => CreateQueue();

        IAspNetCoreSinkQueue IAspNetCoreBulkSink.CreateQueue()
            => CreateQueue();

        public AspNetCoreGoogleSink(AspNetCoreGoogleLoggingContext context)
            => _context = context ?? throw new ArgumentNullException(nameof(context));

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
            AspNetCoreContext context,
            bool isRequestSummary)
        {
            var logEntry = new LogEntry
            {
                LogName = _context.LogName.ToString(),
                Resource = _context.Resource,
                Severity = GetLogSeverity(logLevel),
                Timestamp = global::Google.Protobuf.WellKnownTypes.Timestamp.FromDateTimeOffset(timestamp)
            };
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

        public AspNetCoreGoogleSinkQueue CreateQueue()
            => new AspNetCoreGoogleSinkQueue(this);

        public ValueTask LogAsync<TState>(LogMessage<TState> message, CancellationToken cancellationToken = default)
            => SendAsync(new [] { CreateLogEntry(message) }, cancellationToken);

        public ValueTask LogAsync<TState>(AspNetCoreLogMessage<TState> message, CancellationToken cancellationToken = default)
            => SendAsync(new [] { CreateLogEntry(message) }, cancellationToken);

        public ValueTask DisposeAsync() => default;
    }
}
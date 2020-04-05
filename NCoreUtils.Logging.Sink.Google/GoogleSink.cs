using System;
using System.Buffers;
using System.Threading;
using System.Threading.Tasks;
using Google.Cloud.Logging.V2;

namespace NCoreUtils.Logging.Google
{
    public class GoogleSink : GoogleSinkBase, IBulkSink
    {
        private readonly GoogleLoggingContext _context;

        ISinkQueue IBulkSink.CreateQueue()
            => CreateQueue();

        public GoogleSink(GoogleLoggingContext context)
            => _context = context ?? throw new ArgumentNullException(nameof(context));

        internal LogEntry CreateLogEntry<TState>(LogMessage<TState> message)
        {
            using var buffer = MemoryPool<char>.Shared.Rent(64 * 1024);
            var textPayload = CreateTextPayload(buffer.Memory.Span, message.EventId, message.Category, message.Formatter(message.State, message.Exception), message.Exception?.ToString());
            return new LogEntry
            {
                LogName = _context.LogName.ToString(),
                Resource = _context.Resource,
                Severity = GetLogSeverity(message.LogLevel),
                TextPayload = textPayload,
                Timestamp = global::Google.Protobuf.WellKnownTypes.Timestamp.FromDateTimeOffset(message.Timestamp)
            };
        }

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

        public GoogleSinkQueue CreateQueue()
            => new GoogleSinkQueue(this);

        public ValueTask LogAsync<TState>(LogMessage<TState> message, CancellationToken cancellationToken = default)
            => SendAsync(new [] { CreateLogEntry(message) }, cancellationToken);

        public ValueTask DisposeAsync() => default;
    }
}
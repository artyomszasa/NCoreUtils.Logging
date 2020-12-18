using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace NCoreUtils.Logging
{
    public abstract class LogMessage
    {
        public DateTimeOffset Timestamp { get; } = DateTimeOffset.UtcNow;

        public string Category { get; }

        public LogLevel LogLevel { get; }

        public EventId EventId { get; }

        public Exception? Exception { get; }

        protected LogMessage(string category, LogLevel logLevel, EventId eventId, Exception? exception)
        {
            Category = category;
            LogLevel = logLevel;
            EventId = eventId;
            Exception = exception;
        }

        public abstract ValueTask LogAsync(ISink sink, CancellationToken cancellationToken = default);

        public abstract void Enqueue(ISinkQueue queue);
    }

    public class LogMessage<TState> : LogMessage
    {
        public TState State { get; }

        public Func<TState, Exception?, string> Formatter { get; }

        public LogMessage(
            string category,
            LogLevel logLevel,
            EventId eventId,
            Exception? exception,
            TState state,
            Func<TState, Exception?, string> formatter)
            : base(category, logLevel, eventId, exception)
        {
            State = state;
            Formatter = formatter;
        }

        public override ValueTask LogAsync(ISink sink, CancellationToken cancellationToken = default)
            => sink.LogAsync(this);

        public override void Enqueue(ISinkQueue queue)
            => queue.Enqueue(this);
    }
}
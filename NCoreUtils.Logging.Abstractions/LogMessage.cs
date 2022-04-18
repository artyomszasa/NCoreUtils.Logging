using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace NCoreUtils.Logging
{
    public abstract class LogMessage : IDisposable
    {
        public DateTimeOffset Timestamp { get; private set; } = DateTimeOffset.UtcNow;

        public string Category { get; private set; }

        public LogLevel LogLevel { get; private set; }

        public EventId EventId { get; private set; }

        public Exception? Exception { get; private set; }

        protected LogMessage(string category, LogLevel logLevel, EventId eventId, Exception? exception)
        {
            Category = category;
            LogLevel = logLevel;
            EventId = eventId;
            Exception = exception;
        }

        protected LogMessage Update(string category, LogLevel logLevel, EventId eventId, Exception? exception)
        {
            Category = category;
            LogLevel = logLevel;
            EventId = eventId;
            Exception = exception;
            return this;
        }

        public abstract ValueTask LogAsync(ISink sink, CancellationToken cancellationToken = default);

        public abstract void Enqueue(ISinkQueue queue);

        public virtual void Dispose() { /* noop */ }
    }

    public class LogMessage<TState> : LogMessage
    {
        public TState State { get; private set; }

        public Func<TState, Exception?, string> Formatter { get; private set; }

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

        protected LogMessage<TState> Update(
            string category,
            LogLevel logLevel,
            EventId eventId,
            Exception? exception,
            TState state,
            Func<TState, Exception?, string> formatter)
        {
            base.Update(category, logLevel, eventId, exception);
            State = state;
            Formatter = formatter;
            return this;
        }

        public override ValueTask LogAsync(ISink sink, CancellationToken cancellationToken = default)
            => sink.LogAsync(this, cancellationToken);

        public override void Enqueue(ISinkQueue queue)
            => queue.Enqueue(this);
    }
}
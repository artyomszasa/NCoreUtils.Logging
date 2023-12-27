using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace NCoreUtils.Logging;

public abstract class LogMessage(string category, LogLevel logLevel, EventId eventId, Exception? exception) : IDisposable
{
    public DateTimeOffset Timestamp { get; private set; } = DateTimeOffset.UtcNow;

    public string Category { get; private set; } = category;

    public LogLevel LogLevel { get; private set; } = logLevel;

    public EventId EventId { get; private set; } = eventId;

    public Exception? Exception { get; private set; } = exception;

    protected LogMessage Update(string category, LogLevel logLevel, EventId eventId, Exception? exception)
    {
        Timestamp = DateTimeOffset.UtcNow;
        Category = category;
        LogLevel = logLevel;
        EventId = eventId;
        Exception = exception;
        return this;
    }

    public abstract ValueTask LogAsync(ISink sink, CancellationToken cancellationToken = default);

    public abstract void Enqueue(ISinkQueue queue);

    protected virtual void Dispose(bool disposing) { /* noop */ }

    public void Dispose()
    {
        GC.SuppressFinalize(this);
        Dispose(disposing: true);
    }
}

public class LogMessage<TState>(
    string category,
    LogLevel logLevel,
    EventId eventId,
    Exception? exception,
    TState state,
    Func<TState, Exception?, string> formatter)
    : LogMessage(category, logLevel, eventId, exception)
{
    public TState State { get; private set; } = state;

    public Func<TState, Exception?, string> Formatter { get; private set; } = formatter;

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
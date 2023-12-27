using System;
using Microsoft.Extensions.Logging;
using NCoreUtils.Logging.Internal;

namespace NCoreUtils.Logging;

public class Logger(LoggerProvider provider, string categoryName) : ILogger
{
    internal readonly ScopeStack _stack;

    public LoggerProvider Provider { get; } = provider ?? throw new ArgumentNullException(nameof(provider));

    public string CategoryName { get; } = categoryName;

    public virtual void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        => Provider.PushMessage(new LogMessage<TState>(
            category: CategoryName,
            logLevel: logLevel,
            eventId: eventId,
            exception: exception,
            state: state,
            formatter: formatter
        ));

    public bool IsEnabled(LogLevel logLevel)
        => true;

    public IDisposable BeginScope<TState>(TState state)
#if NET7_0_OR_GREATER
        where TState : notnull
#endif
    {
        bool success;
        int index;
        do
        {
            var scope = _stack.Root;
            index = Scope.Count(scope);
            success = scope == _stack.CompareExchange(Scope.Append(scope, state), scope);
        }
        while (!success);
        return new LoggerScope(this, index);
    }
}
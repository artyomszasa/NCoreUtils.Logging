using System;
using Microsoft.Extensions.Logging;

namespace NCoreUtils.Logging
{
    public class Logger : ILogger
    {
        internal readonly ScopeStack _stack;

        public LoggerProvider Provider { get; }

        public string CategoryName { get; }

        public Logger(LoggerProvider provider, string categoryName)
        {
            Provider = provider ?? throw new ArgumentNullException(nameof(provider));
            CategoryName = categoryName;
        }

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
        {
            bool success;
            var index = 0;
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
}
using System;
using System.Threading;

namespace NCoreUtils.Logging.Internal;

internal class LoggerScope(Logger logger, int index) : IDisposable
{
    private readonly Logger _logger = logger ?? throw new ArgumentNullException(nameof(logger));

    private readonly int _index = index;

    private int _isDisposed;

    public void Dispose()
    {
        if (0 == Interlocked.CompareExchange(ref _isDisposed, 1, 0))
        {
            bool success;
            do
            {
                var stack = _logger._stack.Root;
                if (Scope.Count(stack) >= _index)
                {
                    success = ReferenceEquals(stack, _logger._stack.CompareExchange(Scope.Truncate(stack, _index), stack));
                }
                else
                {
                    success = true;
                }
            }
            while (!success);
        }
    }
}
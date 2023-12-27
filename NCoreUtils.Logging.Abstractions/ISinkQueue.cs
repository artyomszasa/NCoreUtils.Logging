using System;
using System.Threading;
using System.Threading.Tasks;

namespace NCoreUtils.Logging;

public interface ISinkQueue : IDisposable, IAsyncDisposable
{
    void Enqueue<TState>(LogMessage<TState> message);

    ValueTask FlushAsync(CancellationToken cancellationToken = default);
}
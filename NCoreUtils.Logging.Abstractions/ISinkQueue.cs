using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace NCoreUtils.Logging
{
    public interface ISinkQueue : IAsyncDisposable
    {
        void Enqueue<TState>(LogMessage<TState> message);

        ValueTask FlushAsync(CancellationToken cancellationToken = default);
    }
}
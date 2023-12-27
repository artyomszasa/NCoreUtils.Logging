using System;
using System.Threading;
using System.Threading.Tasks;

namespace NCoreUtils.Logging;

public interface ISink : IDisposable, IAsyncDisposable
{
    ValueTask LogAsync<TState>(LogMessage<TState> message, CancellationToken cancellationToken = default);
}
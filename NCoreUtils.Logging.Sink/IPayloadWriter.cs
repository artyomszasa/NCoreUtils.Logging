using System;
using System.Threading;
using System.Threading.Tasks;

namespace NCoreUtils.Logging
{
    public interface IPayloadWriter<TPayload> : IDisposable, IAsyncDisposable
    {
        ValueTask WritePayloadAsync(TPayload payload, CancellationToken cancellationToken = default);
    }
}
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace NCoreUtils.Logging
{
    public interface IBulkPayloadWriter<TPayload> : IPayloadWriter<TPayload>
    {
        ValueTask WritePayloadsAsync(IEnumerable<TPayload> payloads, CancellationToken cancellationToken = default);
    }
}
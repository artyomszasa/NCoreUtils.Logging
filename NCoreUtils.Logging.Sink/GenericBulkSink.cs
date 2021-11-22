using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace NCoreUtils.Logging
{
    public class GenericBulkSink<TPayload> : Internal.GenericSinkBase<TPayload, IBulkPayloadWriter<TPayload>>, IBulkSink
    {
        public GenericBulkSink(IBulkPayloadWriter<TPayload> payloadWriter, IPayloadFactory<TPayload> payloadFactory)
            : base(payloadWriter, payloadFactory)
        { }

        protected internal virtual ValueTask WritePayloadsAsync(IEnumerable<TPayload> payloads, CancellationToken cancellationToken)
            => PayloadWriter.WritePayloadsAsync(payloads, cancellationToken);

        public virtual ISinkQueue CreateQueue()
            => new GenericSinkQueue<TPayload>(this);
    }
}
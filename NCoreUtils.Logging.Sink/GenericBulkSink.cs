namespace NCoreUtils.Logging;

public class GenericBulkSink<TPayload>(IBulkPayloadWriter<TPayload> payloadWriter, IPayloadFactory<TPayload> payloadFactory)
    : Internal.GenericSinkBase<TPayload, IBulkPayloadWriter<TPayload>>(payloadWriter, payloadFactory)
    , IBulkSink
{
    protected internal virtual ValueTask WritePayloadsAsync(IEnumerable<TPayload> payloads, CancellationToken cancellationToken)
        => PayloadWriter.WritePayloadsAsync(payloads, cancellationToken);

    public virtual ISinkQueue CreateQueue()
        => new GenericSinkQueue<TPayload>(this);
}
namespace NCoreUtils.Logging;

public interface IBulkPayloadWriter<TPayload> : IPayloadWriter<TPayload>
{
    ValueTask WritePayloadsAsync(IEnumerable<TPayload> payloads, CancellationToken cancellationToken = default);
}
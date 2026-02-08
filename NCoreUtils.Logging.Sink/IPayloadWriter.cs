namespace NCoreUtils.Logging;

public interface IPayloadWriter<TPayload> : IDisposable, IAsyncDisposable
{
    ValueTask WritePayloadAsync(TPayload payload, CancellationToken cancellationToken = default);
}
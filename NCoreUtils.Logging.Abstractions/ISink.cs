namespace NCoreUtils.Logging;

public interface ISink : IDisposable, IAsyncDisposable
{
    ValueTask LogAsync<TState>(LogMessage<TState> message, CancellationToken cancellationToken = default);
}
namespace NCoreUtils.Logging;

public interface IByteSequence : IDisposable, IAsyncDisposable
{
    ValueTask WriteToAsync(IByteSequenceOutput output, CancellationToken cancellationToken = default);
}
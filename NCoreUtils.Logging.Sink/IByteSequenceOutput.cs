namespace NCoreUtils.Logging;

public interface IByteSequenceOutput : IAsyncDisposable, IDisposable
{
    Stream GetStream()
#if NETFRAMEWORK
        ;
#else
        => new Internal.ByteSequenceOutputStream(this);
#endif

    ValueTask WriteAsync(ReadOnlyMemory<byte> data, CancellationToken cancellationToken = default);
}
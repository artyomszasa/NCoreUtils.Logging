using System.Buffers;

namespace NCoreUtils.Logging.ByteSequences;

public class MemoryOwnerAsByteSequence(IMemoryOwner<byte> owner, int size) : IByteSequence
{
    public IMemoryOwner<byte> Owner { get; } = owner;

    public int Size { get; } = size;

    public ValueTask WriteToAsync(IByteSequenceOutput output, CancellationToken cancellationToken = default)
        => output.WriteAsync(Owner.Memory[..Math.Min(Size, Owner.Memory.Length)], cancellationToken);

    #region disposable

    protected virtual void Dispose(bool disposing)
    {
        if (disposing)
        {
            Owner.Dispose();
        }
    }

    protected virtual ValueTask DisposeAsyncCore()
    {
        Owner.Dispose();
        return default;
    }

    public void Dispose()
    {
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }

    public async ValueTask DisposeAsync()
    {
        GC.SuppressFinalize(this);
        await DisposeAsyncCore();
        Dispose(disposing: false);
    }

    #endregion
}
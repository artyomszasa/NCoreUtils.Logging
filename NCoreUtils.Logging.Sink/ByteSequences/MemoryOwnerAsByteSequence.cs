using System;
using System.Buffers;
using System.Threading;
using System.Threading.Tasks;

namespace NCoreUtils.Logging.ByteSequences
{
    public class MemoryOwnerAsByteSequence : IByteSequence
    {
        public IMemoryOwner<byte> Owner { get; }

        public int Size { get; }

        public MemoryOwnerAsByteSequence(IMemoryOwner<byte> owner, int size)
        {
            Owner = owner;
            Size = size;
        }

        public ValueTask WriteToAsync(IByteSequenceOutput output, CancellationToken cancellationToken = default)
            => output.WriteAsync(Owner.Memory[..Math.Min(Size, Owner.Memory.Length)], cancellationToken);

        #region disposable

        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        public async ValueTask DisposeAsync()
        {
            await DisposeAsyncCore().ConfigureAwait(false);
            Dispose(disposing: false);
#pragma warning disable CA1816 // Dispose methods should call SuppressFinalize
            GC.SuppressFinalize(this);
#pragma warning restore CA1816 // Dispose methods should call SuppressFinalize
        }

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

        #endregion
    }
}
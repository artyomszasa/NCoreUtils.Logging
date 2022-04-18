using System;
using System.Buffers;
using System.Threading;
using System.Threading.Tasks;

namespace NCoreUtils.Logging.FormattedString.Internal
{
    public sealed class InMemoryByteSequence : IByteSequence
    {
        private int _isDisposed;

        private IMemoryOwner<byte> Owner { get; }

        private int Size { get; }

        public ReadOnlyMemory<byte> Memory => Owner.Memory[..Size];

        public InMemoryByteSequence(IMemoryOwner<byte> owner, int size)
        {
            if (size < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(size));
            }
            Owner = owner ?? throw new ArgumentNullException(nameof(owner));
            Size = size;
        }

        public ValueTask WriteToAsync(IByteSequenceOutput output, CancellationToken cancellationToken = default)
            => output.WriteAsync(Memory, cancellationToken);

        public void Dispose()
        {
            if (0 == Interlocked.CompareExchange(ref _isDisposed, 1, 0))
            {
                Owner.Dispose();
            }
        }

        public ValueTask DisposeAsync()
        {
            Dispose();
            return default;
        }
    }
}
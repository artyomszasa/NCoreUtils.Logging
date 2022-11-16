using System;
using System.Buffers;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace NCoreUtils.Logging.Internal
{
    public sealed class ByteSequenceOutputStream : Stream
    {
        public IByteSequenceOutput Output { get; }

        public override bool CanRead => false;

        public override bool CanSeek => false;

        public override bool CanWrite => true;

        public override long Length => throw new NotSupportedException();

        public override long Position { get => throw new NotSupportedException(); set => throw new NotSupportedException(); }

        public ByteSequenceOutputStream(IByteSequenceOutput output)
            => Output = output ?? throw new ArgumentNullException(nameof(output));

        public override void Flush() { /* noop */ }

        public override int Read(byte[] buffer, int offset, int count)
            => throw new NotSupportedException();

        public override long Seek(long offset, SeekOrigin origin)
            => throw new NotSupportedException();

        public override void SetLength(long value)
            => throw new NotSupportedException();

        public override void Write(byte[] buffer, int offset, int count)
            => WriteAsync(buffer, offset, count).GetAwaiter().GetResult();

        public
#if !NETSTANDARD2_0
        override
#endif
        void Write(ReadOnlySpan<byte> buffer)
        {
            var array = ArrayPool<byte>.Shared.Rent(buffer.Length);
            try
            {
                buffer.CopyTo(array);
                Write(array, 0, buffer.Length);
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(array);
            }
        }

        public override Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
            => WriteAsync(buffer.AsMemory().Slice(offset, count), cancellationToken).AsTask();

        public
#if !NETSTANDARD2_0
        override
#endif
        ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = default)
            => Output.WriteAsync(buffer, cancellationToken);
    }
}
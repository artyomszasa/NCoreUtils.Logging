using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace NCoreUtils.Logging
{
    public interface IByteSequenceOutput : IAsyncDisposable, IDisposable
    {
        Stream GetStream()
            => new Internal.ByteSequenceOutputStream(this);

        ValueTask WriteAsync(ReadOnlyMemory<byte> data, CancellationToken cancellationToken = default);
    }
}
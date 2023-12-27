using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace System;

internal static class StreamCompatExtensions
{
    public static async ValueTask WriteAsync(this Stream stream, ReadOnlyMemory<byte> data, CancellationToken cancellationToken = default)
    {
        var buffer = Buffers.ArrayPool<byte>.Shared.Rent(data.Length);
        try
        {
            data.Span.CopyTo(buffer.AsSpan());
            await stream.WriteAsync(buffer, 0, data.Length, cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            Buffers.ArrayPool<byte>.Shared.Return(buffer);
        }
    }

    public static ValueTask DisposeAsync(this Stream stream)
    {
        stream.Dispose();
        return default;
    }
}
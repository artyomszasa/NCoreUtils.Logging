using System;
using System.Threading;
using System.Threading.Tasks;

namespace NCoreUtils.Logging.FormattedString
{
    public interface IOutputStreamWrapper : IAsyncDisposable, IDisposable
    {
        ValueTask WriteAsync(ReadOnlyMemory<byte> data, CancellationToken cancellationToken = default);
    }
}
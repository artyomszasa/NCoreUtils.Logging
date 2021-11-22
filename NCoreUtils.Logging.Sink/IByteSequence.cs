using System;
using System.Threading;
using System.Threading.Tasks;

namespace NCoreUtils.Logging
{
    public interface IByteSequence : IDisposable, IAsyncDisposable
    {
        ValueTask WriteToAsync(IByteSequenceOutput output, CancellationToken cancellationToken = default);
    }
}
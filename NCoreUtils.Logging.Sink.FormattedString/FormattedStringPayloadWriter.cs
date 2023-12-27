using System;
using System.Threading;
using System.Threading.Tasks;
using NCoreUtils.Logging.FormattedString.Internal;

namespace NCoreUtils.Logging.FormattedString;

public class FormattedStringPayloadWriter(IByteSequenceOutput output) : IPayloadAsByteSequenceWriter<InMemoryByteSequence>
{
    public IByteSequenceOutput Output { get; } = output ?? throw new ArgumentNullException(nameof(output));

    public ValueTask<IByteSequence> CreateByteSequenceAsync(InMemoryByteSequence payload, CancellationToken cancellationToken = default)
        => new(payload);

#if NETFRAMEWORK
    async ValueTask IPayloadWriter<InMemoryByteSequence>.WritePayloadAsync(
        InMemoryByteSequence payload,
        CancellationToken cancellationToken)
    {
        using var sequence = await CreateByteSequenceAsync(payload, cancellationToken)
            .ConfigureAwait(false);
        await sequence.WriteToAsync(Output, cancellationToken).ConfigureAwait(false);
    }
#endif

    #region disposable

    protected virtual void Dispose(bool disposing) { /* noop */ }

    protected virtual ValueTask DisposeAsyncCore() => default;

    public void Dispose()
    {
        GC.SuppressFinalize(this);
        Dispose(true);
    }

    public async ValueTask DisposeAsync()
    {
        await DisposeAsyncCore();
        Dispose(disposing: false);
        GC.SuppressFinalize(this);
    }

    #endregion
}
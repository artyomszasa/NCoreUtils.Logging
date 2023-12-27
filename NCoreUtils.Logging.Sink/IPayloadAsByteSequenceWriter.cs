using System.Buffers;
using System.Threading;
using System.Threading.Tasks;

namespace NCoreUtils.Logging
{
    public interface IPayloadAsByteSequenceWriter<TPayload> : IPayloadWriter<TPayload>
    {
        IByteSequenceOutput Output { get; }

        ValueTask<IByteSequence> CreateByteSequenceAsync(
            TPayload payload,
            CancellationToken cancellationToken = default
        );

#if !NETFRAMEWORK
        async ValueTask IPayloadWriter<TPayload>.WritePayloadAsync(
            TPayload payload,
            CancellationToken cancellationToken)
        {
            using var sequence = await CreateByteSequenceAsync(payload, cancellationToken)
                .ConfigureAwait(false);
            await sequence.WriteToAsync(Output, cancellationToken).ConfigureAwait(false);
        }
#endif
    }
}
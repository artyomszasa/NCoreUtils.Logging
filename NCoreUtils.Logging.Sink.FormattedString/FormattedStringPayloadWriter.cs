using System;
using System.Buffers;
using System.Threading;
using System.Threading.Tasks;

namespace NCoreUtils.Logging.FormattedString
{
    public class FormattedStringPayloadWriter : IPayloadWriter<(IMemoryOwner<byte> Owner, int Size)>
    {
        private IOutputStreamWrapper Output { get; }

        public FormattedStringPayloadWriter(IOutputStreamWrapper output)
            => Output = output ?? throw new ArgumentNullException(nameof(output));

        public async ValueTask WritePayloadAsync((IMemoryOwner<byte> Owner, int Size) payload, CancellationToken cancellationToken = default)
        {
            try
            {
                await Output.WriteAsync(payload.Owner.Memory[..payload.Size], cancellationToken);
            }
            finally
            {
                payload.Owner.Dispose();
            }
        }

        public void Dispose()
            => Output.Dispose();

        public ValueTask DisposeAsync()
            => Output.DisposeAsync();
    }
}
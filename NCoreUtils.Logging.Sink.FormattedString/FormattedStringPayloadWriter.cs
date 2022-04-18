using System;
using System.Threading;
using System.Threading.Tasks;
using NCoreUtils.Logging.FormattedString.Internal;

namespace NCoreUtils.Logging.FormattedString
{
    public class FormattedStringPayloadWriter : IPayloadAsByteSequenceWriter<InMemoryByteSequence>
    {
        public IByteSequenceOutput Output { get; }

        public FormattedStringPayloadWriter(IByteSequenceOutput output)
            => Output = output ?? throw new ArgumentNullException(nameof(output));

        public ValueTask<IByteSequence> CreateByteSequenceAsync(InMemoryByteSequence payload, CancellationToken cancellationToken = default)
            => new(payload);

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
}
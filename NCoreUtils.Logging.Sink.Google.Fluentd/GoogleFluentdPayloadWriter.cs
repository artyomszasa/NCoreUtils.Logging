using System;
using System.Threading;
using System.Threading.Tasks;
using NCoreUtils.Logging.ByteSequences;
using NCoreUtils.Logging.Google.Data;

namespace NCoreUtils.Logging.Google
{
    public class GoogleFluentdPayloadWriter : IPayloadAsByteSequenceWriter<LogEntry>
    {
        private int _isDisposed;

        protected IGoogleFluentdSinkConfiguration Configuration { get; }

        public IByteSequenceOutput Output { get; }

        public GoogleFluentdPayloadWriter(IGoogleFluentdSinkConfiguration configuration, IByteSequenceOutput output)
        {
            Configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
            Output = output ?? throw new ArgumentNullException(nameof(output));
        }

        public ValueTask<IByteSequence> CreateByteSequenceAsync(LogEntry payload, CancellationToken cancellationToken = default)
        {
            if (0 != Interlocked.CompareExchange(ref _isDisposed, 0, 0))
            {
                throw new ObjectDisposedException(nameof(GoogleFluentdPayloadWriter));
            }
            return new ValueTask<IByteSequence>(new JsonSerializedByteSequence<LogEntry>(payload, LogEntryJsonContext.Default.LogEntry));
        }

        #region disposable

        protected virtual void Dispose(bool disposing)
        {
            // NOTE: no unmanaged disposables
            if (0 == Interlocked.CompareExchange(ref _isDisposed, 1, 0) && disposing)
            {
                Output.Dispose();
            }
        }

        /// <summary>
        /// Only disposes managed resources.
        /// </summary>
        protected virtual ValueTask DisposeAsyncCore()
        {
            if (0 == Interlocked.CompareExchange(ref _isDisposed, 1, 0))
            {
                return Output.DisposeAsync();
            }
            return default;
        }

        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
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
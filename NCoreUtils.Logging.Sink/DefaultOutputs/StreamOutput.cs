using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace NCoreUtils.Logging.DefaultOutputs
{
    public abstract class StreamOutput : IByteSequenceOutput
    {
        private Stream? _stream;

        protected Stream Stream
        {
            get
            {
                _stream ??= InitializeStream();
                return _stream;
            }
        }

        protected abstract Stream InitializeStream();

        public Stream GetStream()
            => Stream;

        public ValueTask WriteAsync(ReadOnlyMemory<byte> data, CancellationToken cancellationToken = default)
            => Stream.WriteAsync(data, cancellationToken);

        #region disposable

        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        public async ValueTask DisposeAsync()
        {
            await DisposeAsyncCore().ConfigureAwait(false);
            Dispose(disposing: false);
#pragma warning disable CA1816 // Dispose methods should call SuppressFinalize
            GC.SuppressFinalize(this);
#pragma warning restore CA1816 // Dispose methods should call SuppressFinalize
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                _stream?.Dispose();
            }
        }

        protected virtual ValueTask DisposeAsyncCore()
#if NETSTANDARD2_0
        {
            _stream?.Dispose();
            return default;
        }
#else
            => _stream?.DisposeAsync() ?? default;
#endif

        #endregion
    }
}
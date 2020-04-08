using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace NCoreUtils.Logging.Google.Fluentd
{
    public class StreamFluentdSink : IFluentdSink
    {
        static readonly UTF8Encoding _utf8 = new UTF8Encoding(false);

        readonly StreamWriter _stream;

        int _isDiposed;

        public StreamFluentdSink(Stream stream, bool leaveOpen = false)
        {
            _stream = new StreamWriter(stream ?? throw new ArgumentNullException(nameof(stream)), _utf8, 8 * 1024, leaveOpen);
        }

        private void ThrowIfDisposed()
        {
            if (0 != Interlocked.CompareExchange(ref _isDiposed, 0, 0))
            {
                throw new ObjectDisposedException(nameof(StreamFluentdSink));
            }
        }

        protected virtual ValueTask DisposeAsync(bool disposing)
        {
            if (0 == Interlocked.CompareExchange(ref _isDiposed, 1, 0))
            {
                if (disposing)
                {
                    return _stream.DisposeAsync();
                }
            }
            return default;
        }

        public ValueTask WriteAsync(string entry, CancellationToken cancellationToken)
        {
            ThrowIfDisposed();
            return new ValueTask(_stream.WriteLineAsync(entry));
        }

        public ValueTask DisposeAsync()
        {
            GC.SuppressFinalize(this);
            return DisposeAsync(true);
        }
    }
}
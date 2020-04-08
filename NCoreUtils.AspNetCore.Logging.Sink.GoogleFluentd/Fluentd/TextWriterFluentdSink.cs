using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace NCoreUtils.Logging.Google.Fluentd
{
    public class TextWriterFluentdSink : IFluentdSink
    {
        static readonly UTF8Encoding _utf8 = new UTF8Encoding(false);

        readonly TextWriter _stream;

        readonly bool _leaveOpen;

        int _isDiposed;

        public TextWriterFluentdSink(TextWriter stream, bool leaveOpen = false)
        {
            _stream = stream;
            _leaveOpen = leaveOpen;
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
                    if (!_leaveOpen)
                    {
                        return _stream.DisposeAsync();
                    }
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
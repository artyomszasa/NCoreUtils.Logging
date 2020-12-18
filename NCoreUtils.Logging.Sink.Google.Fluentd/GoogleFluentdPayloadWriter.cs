using System;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using NCoreUtils.Logging.Google.Data;

namespace NCoreUtils.Logging.Google
{
    public class GoogleFluentdPayloadWriter : IPayloadWriter<LogEntry>
    {
        private static readonly byte[] _eol = Encoding.ASCII.GetBytes(Environment.NewLine);

        private int _isDisposed;

        protected IGoogleFluentdSinkConfiguration Configuration { get; }

        protected Stream? OutputStream { get; private set; }

        public GoogleFluentdPayloadWriter(IGoogleFluentdSinkConfiguration configuration)
        {
            Configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        }

        private async ValueTask<Stream> OpenOutputStreamAsync(CancellationToken cancellationToken)
        {
            if (OutputStream is null)
            {
                // NOTE: when used correctly no concurrency issue should occure...
                OutputStream = await Configuration.CreateOutputStreamAsync(cancellationToken);
            }
            return OutputStream;
        }

        #region IDisposable

        protected virtual void Dispose(bool disposing)
        {
            // NOTE: no unmanaged disposables
            if (0 == Interlocked.CompareExchange(ref _isDisposed, 1, 0) && disposing)
            {
                OutputStream?.Dispose();
            }
        }

        /// <summary>
        /// Only disposes managed resources.
        /// </summary>
        protected virtual ValueTask DisposeAsyncCore()
        {
            if (0 == Interlocked.CompareExchange(ref _isDisposed, 1, 0))
            {
                #if NETSTANDARD2_1
                return OutputStream?.DisposeAsync() ?? default;
                #else
                OutputStream?.Dispose();
                #endif
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

        public virtual async ValueTask WritePayloadAsync(LogEntry payload, CancellationToken cancellationToken = default)
        {
            var stream = await OpenOutputStreamAsync(cancellationToken);
            await JsonSerializer.SerializeAsync(stream, payload, Configuration.JsonSerializerOptions, cancellationToken);
            #if NETSTANDARD2_1
            await stream.WriteAsync(_eol.AsMemory(), CancellationToken.None);
            #else
            await stream.WriteAsync(_eol, 0, _eol.Length, CancellationToken.None);
            #endif
            await stream.FlushAsync(CancellationToken.None);
        }
    }
}
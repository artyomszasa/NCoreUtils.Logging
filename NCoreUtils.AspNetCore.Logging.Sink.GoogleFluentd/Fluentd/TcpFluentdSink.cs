using System;
using System.Buffers;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace NCoreUtils.Logging.Google.Fluentd
{
    public class TcpFluentdSink : IFluentdSink
    {
        static readonly UTF8Encoding _utf8 = new UTF8Encoding(false);

        private readonly IPEndPoint _endpoint;

        private readonly Socket _socket;

        int _isDisposed;

        public TcpFluentdSink(IPEndPoint endpoint)
        {
            _endpoint = endpoint;
            _socket = new Socket(endpoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
            _socket.Blocking = false;
            _socket.NoDelay = true;
        }

        private ValueTask EnsureConnected(CancellationToken cancellationToken)
        {
            if (_socket.Connected)
            {
                return default;
            }
            // NOTE: no SocketAsyncEventArgs pooling as it should be a one-time operation...
            return new ValueTask(_socket.ConnectAsync(_endpoint));
        }

        protected virtual ValueTask DisposeAsync(bool disposing)
        {
            if (0 == Interlocked.CompareExchange(ref _isDisposed, 1, 0))
            {
                if (disposing)
                {
                    // FIXME: disconnect (?)
                    _socket.Shutdown(SocketShutdown.Both);
                    _socket.Dispose();
                }
            }
            return default;
        }

        public ValueTask DisposeAsync()
        {
            GC.SuppressFinalize(this);
            return DisposeAsync(true);
        }

        public async ValueTask WriteAsync(string entry, CancellationToken cancellationToken)
        {
            await EnsureConnected(cancellationToken);
            var buffer = new byte[32 * 1024];
            var totalBytes = _utf8.GetBytes(entry, buffer);
            buffer[totalBytes++] = 0x0a; // EOL
            var totalSent = 0;
            while (totalSent < totalBytes)
            {
                var completion = new TaskCompletionSource<int>();
                using var args = new SocketAsyncEventArgs();
                args.SetBuffer(buffer, 0, totalBytes);
                args.Completed += (_, e) => completion.SetResult(e.BytesTransferred);
                int sent;
                if (_socket.SendAsync(args))
                {
                    sent = await completion.Task.ConfigureAwait(false);
                }
                else
                {
                    sent = args.BytesTransferred;
                }
                totalSent += sent;
            }
        }
    }
}
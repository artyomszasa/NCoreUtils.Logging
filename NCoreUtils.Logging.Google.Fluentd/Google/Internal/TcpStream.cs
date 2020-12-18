using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace NCoreUtils.Logging.Google.Internal
{
    public class TcpStream : Stream
    {
        private readonly EndPoint _endpoint;

        private readonly Socket _socket;

        public override bool CanRead => false;

        public override bool CanSeek => false;

        public override bool CanWrite => true;

        public override long Length => throw new NotSupportedException();

        public override long Position
        {
            get => throw new NotSupportedException();
            set => throw new NotSupportedException();
        }

        public TcpStream(IPEndPoint endpoint)
        {
            _endpoint = endpoint;
            _socket = new Socket(endpoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp)
            {
                Blocking = false,
                NoDelay = true
            };
        }

        private ValueTask EnsureConnected(CancellationToken cancellationToken)
        {
            if (_socket.Connected)
            {
                return default;
            }
            cancellationToken.ThrowIfCancellationRequested();
            // NOTE: no SocketAsyncEventArgs pooling as it should be a one-time operation...
            return new ValueTask(_socket.ConnectAsync(_endpoint));
        }

        [DoesNotReturn]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void ThrowSynchronousOperationNotSupported()
            => ThrowSynchronousOperationNotSupported<int>();

        [DoesNotReturn]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private T ThrowSynchronousOperationNotSupported<T>()
            => throw new NotSupportedException("Synchronous operations are not supported.");

        public override void Flush()
            => ThrowSynchronousOperationNotSupported();

        public override Task FlushAsync(CancellationToken cancellationToken)
            => Task.CompletedTask;


#if NETSTANDARD2_1
        public override int Read(Span<byte> buffer)
            => ThrowSynchronousOperationNotSupported<int>();
#endif

        public override int Read(byte[] buffer, int offset, int count)
            => ThrowSynchronousOperationNotSupported<int>();

#if NETSTANDARD2_1
        public override ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
            => throw new NotSupportedException();
#endif

        public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
            => throw new NotSupportedException();

        public override long Seek(long offset, SeekOrigin origin)
            => throw new NotSupportedException();

        public override void SetLength(long value)
            => throw new NotSupportedException();

        public override void Write(byte[] buffer, int offset, int count)
            => ThrowSynchronousOperationNotSupported();

#if NETSTANDARD2_1
        public override void Write(ReadOnlySpan<byte> buffer)
            => ThrowSynchronousOperationNotSupported();
#endif

#if NETSTANDARD2_1
        public override async ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = default)
        {
            await EnsureConnected(cancellationToken);
            var sent = 0;
            while (sent < buffer.Length)
            {
                sent += await _socket.SendAsync(buffer.Slice(sent), SocketFlags.None, cancellationToken);
            }
        }

        public override Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
            => WriteAsync(buffer.AsMemory().Slice(offset, count), cancellationToken).AsTask();
#else
        public override async Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            await EnsureConnected(cancellationToken);
            var sent = 0;
            while (sent < buffer.Length)
            {
                sent += await _socket.SendAsync(new ArraySegment<byte>(buffer, offset + sent, count - sent), SocketFlags.None);
            }
        }
#endif
    }
}
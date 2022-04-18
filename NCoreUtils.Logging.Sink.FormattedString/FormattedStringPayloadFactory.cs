using System;
using System.Buffers;
using System.Text;
using System.Threading.Tasks;
using NCoreUtils.Logging.FormattedString.Internal;

namespace NCoreUtils.Logging.FormattedString
{
    public class FormattedStringPayloadFactory : IPayloadFactory<InMemoryByteSequence>
    {
        // structure: {timestamp} [{category}] {request summary}? {message}{\n exn}?

        private const string TimestampFormat = "yyyy.MM.dd HH:mm:ss.fff";

        private static Encoding Encoding { get; } = new UTF8Encoding(false);

        private static byte[] NewLine { get; } = Encoding.GetBytes(Environment.NewLine);

        private static byte[] Space { get; } = Encoding.GetBytes(" ");

        private static byte[] Lbrack { get; } = Encoding.GetBytes("[");

        private static byte[] Rbrack { get; } = Encoding.GetBytes("]");

        private static int TimestampSize { get; } = Encoding.GetByteCount(DateTimeOffset.Now.ToString(TimestampFormat));

        private static void WriteTimestamp(Span<byte> destination, in DateTimeOffset timestamp)
        {
            Span<char> buffer = stackalloc char[23];
            timestamp.TryFormat(buffer, out var _, TimestampFormat);
            Encoding.GetBytes(buffer, destination);
        }

        public InMemoryByteSequence CreatePayload<TState>(LogMessage<TState> message)
        {
            // evaluate message if not evaluated yet....
            var textMessage = message.Formatter(message.State, message.Exception);
            var textMessageSize = Encoding.GetByteCount(textMessage ?? string.Empty);
            var categorySize = Encoding.GetByteCount(message.Category ?? string.Empty);
            var payloadSize = TimestampSize
                + Space.Length
                + Lbrack.Length
                + categorySize
                + Rbrack.Length
                + Space.Length;
            // FIXME: request summary
            payloadSize += textMessageSize + NewLine.Length; // message + \n
            // FIXME: avoid exn.ToString()
            var exnString = message.Exception?.ToString();
            if (!string.IsNullOrEmpty(exnString))
            {
                payloadSize += Space.Length * 2 + Encoding.GetByteCount(exnString) + NewLine.Length;
            }
            var payload = MemoryPool<byte>.Shared.Rent(payloadSize);
            var payloadSpan = payload.Memory.Span;
            WriteTimestamp(payloadSpan, message.Timestamp);
            var written = TimestampSize;
            Space.AsSpan().CopyTo(payloadSpan[written..]);
            written += Space.Length;
            Lbrack.AsSpan().CopyTo(payloadSpan[written..]);
            written += Lbrack.Length;
            Encoding.GetBytes(message.Category ?? string.Empty, payloadSpan[written..]);
            written += categorySize;
            Rbrack.AsSpan().CopyTo(payloadSpan[written..]);
            written += Rbrack.Length;
            Space.AsSpan().CopyTo(payloadSpan[written..]);
            written += Space.Length;
            Encoding.GetBytes(textMessage ?? string.Empty, payloadSpan[written..]);
            written += textMessageSize;
            NewLine.AsSpan().CopyTo(payloadSpan[written..]);
            written += NewLine.Length;
            if (!string.IsNullOrEmpty(exnString))
            {
                Space.AsSpan().CopyTo(payloadSpan[written..]);
                written += Space.Length;
                Space.AsSpan().CopyTo(payloadSpan[written..]);
                written += Space.Length;
                written += Encoding.GetBytes(exnString, payloadSpan[written..]);
                NewLine.AsSpan().CopyTo(payloadSpan[written..]);
                written += NewLine.Length;
            }
            return new(payload, payloadSize);
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
#pragma warning disable CA1816 // Dispose methods should call SuppressFinalize
            GC.SuppressFinalize(this);
#pragma warning restore CA1816 // Dispose methods should call SuppressFinalize
        }

        protected virtual void Dispose(bool disposing) { /* noop */ }

        protected virtual ValueTask DisposeAsyncCore()
            => default;
    }
}
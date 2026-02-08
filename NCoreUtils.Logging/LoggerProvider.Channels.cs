using System.Diagnostics.CodeAnalysis;
using System.Threading.Channels;
using NCoreUtils.Logging.Internal;

namespace NCoreUtils.Logging;

public partial class LoggerProvider
{
    private readonly struct QueueReader(ChannelReader<LogMessage> reader)
    {
        public ValueTask<bool> WaitToReadAsync(CancellationToken cancellationToken)
            => reader.WaitToReadAsync(cancellationToken);

        public bool TryRead([MaybeNullWhen(false)] out LogMessage item)
            => reader.TryRead(out item);

        public ValueTask<int> ReadAllAvailableWithinAsync(
            LogMessage[] buffer,
            int index,
            TimeSpan timeout,
            CancellationToken cancellationToken)
            => reader.ReadAllAvailableWithinAsync(buffer, index, timeout, cancellationToken);
    }

    private readonly Channel<LogMessage> _queue = Channel.CreateUnbounded<LogMessage>(new UnboundedChannelOptions
    {
        SingleReader = true,
        SingleWriter = false,
        AllowSynchronousContinuations = false
    });

    private void CompleteQueue()
        => _queue.Writer.Complete();

    private ValueTask PushToQueueAsync(LogMessage message, CancellationToken cancellationToken)
        => _queue.Writer.WriteAsync(message, cancellationToken);

    private QueueReader GetQueueReader()
        => new(_queue.Reader);
}
using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using NCoreUtils.Logging.Internal;

namespace NCoreUtils.Logging;

public partial class LoggerProvider
{
    private readonly struct QueueReader
    {
        private readonly ChannelReader<LogMessage> _reader;

        public QueueReader(ChannelReader<LogMessage> reader)
            => _reader = reader;

        public ValueTask<bool> WaitToReadAsync(CancellationToken cancellationToken)
            => _reader.WaitToReadAsync(cancellationToken);

        public bool TryRead([MaybeNullWhen(false)] out LogMessage item)
            => _reader.TryRead(out item);

        public ValueTask<int> ReadAllAvailableWithinAsync(
            LogMessage[] buffer,
            int index,
            TimeSpan timeout,
            CancellationToken cancellationToken)
            => _reader.ReadAllAvailableWithinAsync(buffer, index, timeout, cancellationToken);
    }

    private readonly Channel<LogMessage> _queue = Channel.CreateUnbounded<LogMessage>(new UnboundedChannelOptions
    {
        SingleReader = true,
        SingleWriter = false,
        AllowSynchronousContinuations = false
    });

    private void CompleteQueue()
    {
        _queue.Writer.Complete();
    }

    private ValueTask PushToQueueAsync(LogMessage message, CancellationToken cancellationToken)
        => _queue.Writer.WriteAsync(message, cancellationToken);

    private QueueReader GetQueueReader()
        => new(_queue.Reader);
}
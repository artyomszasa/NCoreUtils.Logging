using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;

namespace NCoreUtils.Logging;

public partial class LoggerProvider
{
    private class PreloadReader
    {
        private readonly BlockingCollection<LogMessage> _source;

        private SpinLock _sync = new SpinLock(enableThreadOwnerTracking: false);

        private LogMessage? _item;

        private Task<bool>? _pendingRead;

        public PreloadReader(BlockingCollection<LogMessage> source)
            => _source = source;

        private void ResetPendingTask()
        {
            var lockTaken = false;
            _sync.Enter(ref lockTaken);
            try
            {
                _pendingRead = default;
            }
            finally
            {
                if (lockTaken)
                {
                    _sync.Exit();
                }
            }
        }

        private async Task<bool> DoWaitToReadAsync(CancellationToken cancellationToken)
        {
            await Task.Yield();
            try
            {
                LogMessage? item;
                while (!_source.TryTake(out item, 450, cancellationToken))
                {
                    await Task.Yield();
                }
                _item = item;
                return true;
            }
            catch
            {
                return false;
            }
            finally
            {
                ResetPendingTask();
            }
        }

        public ValueTask<bool> WaitToReadAsync(CancellationToken cancellationToken)
        {
            if (_source.IsCompleted)
            {
                return new(false);
            }
            var lockTaken = false;
            _sync.Enter(ref lockTaken);
            try
            {
                // if there is already pending read --> return it
                if (_pendingRead is not null)
                {
                    return new(_pendingRead);
                }
                // if item has been preloaded --> read operation available
                if (_item is not null)
                {
                    return new(true);
                }
                if (_source.TryTake(out var item))
                {
                    _item = item;
                    return new(true);
                }
                return new(_pendingRead = DoWaitToReadAsync(cancellationToken));
            }
            finally
            {
                if (lockTaken)
                {
                    _sync.Exit();
                }
            }
        }

        public bool TryTake([MaybeNullWhen(false)] out LogMessage item)
        {
            var lockTaken = false;
            _sync.Enter(ref lockTaken);
            try
            {
                if (_item is not null)
                {
                    item = _item;
                    _item = null;
                    return true;
                }
                return _source.TryTake(out item);
            }
            finally
            {
                if (lockTaken)
                {
                    _sync.Exit();
                }
            }
        }

        public bool TryTake([MaybeNullWhen(false)] out LogMessage item, int timeoutMs, CancellationToken cancellationToken)
        {
            var lockTaken = false;
            _sync.Enter(ref lockTaken);
            try
            {
                if (_item is not null)
                {
                    item = _item;
                    _item = null;
                    return true;
                }
            }
            finally
            {
                if (lockTaken)
                {
                    _sync.Exit();
                }
            }
            return _source.TryTake(out item, timeoutMs, cancellationToken);
        }
    }

    private struct QueueReader
    {
        private PreloadReader _queue;

        public QueueReader(PreloadReader queue)
            => _queue = queue;

        public ValueTask<bool> WaitToReadAsync(CancellationToken cancellationToken)
            => _queue.WaitToReadAsync(cancellationToken);

        public bool TryRead([MaybeNullWhen(false)] out LogMessage item)
            => _queue.TryTake(out item);

        public ValueTask<int> ReadAllAvailableWithinAsync(
            LogMessage[] buffer,
            int index,
            TimeSpan timeout,
            CancellationToken cancellationToken)
        {
            var timeoutMs = (long)timeout.TotalMilliseconds;
            var stopwatch = new Stopwatch();
            var i = index;
            stopwatch.Start();
            while (true)
            {
                if (i >= buffer.Length)
                {
                    return new(buffer.Length);
                }
                var currentTimeout = timeoutMs - stopwatch.ElapsedMilliseconds;
                if (currentTimeout <= 0)
                {
                    return new(i);
                }
                if (_queue.TryTake(out var item, unchecked((int)currentTimeout), cancellationToken))
                {
                    buffer[i++] = item;
                }
            }
        }
    }

    private readonly BlockingCollection<LogMessage> _queue = new();

    private PreloadReader? _preloadReader;

    private void CompleteQueue()
        => _queue.CompleteAdding();

    private QueueReader GetQueueReader()
        => new(_preloadReader ??= new(_queue));

    private ValueTask PushToQueueAsync(LogMessage message, CancellationToken cancellationToken)
    {
        _queue.Add(message, cancellationToken);
        return default;
    }
}
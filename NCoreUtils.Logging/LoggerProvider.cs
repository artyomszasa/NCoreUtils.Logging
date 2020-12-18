using System;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using NCoreUtils.Logging.Internal;

namespace NCoreUtils.Logging
{
    public class LoggerProvider : ILoggerProvider, IDisposable, IAsyncDisposable
    {
        private readonly Channel<LogMessage> _queue = Channel.CreateUnbounded<LogMessage>(new UnboundedChannelOptions
        {
            SingleReader = true,
            SingleWriter = false,
            AllowSynchronousContinuations = false
        });

        private readonly object _sync = new object();

        private readonly CancellationTokenSource _cancellation = new CancellationTokenSource();

        private readonly ISink _sink;

        private int _isDisposed;

        private Task? _worker;

        public LoggerProvider(ISink sink)
            => _sink = sink ?? throw new ArgumentNullException(nameof(sink));

        ILogger ILoggerProvider.CreateLogger(string categoryName)
            => CreateLogger(categoryName);

        #region IDisposable

        protected virtual void Dispose(bool disposing)
        {
            // NOTE: no unmanaged disposables
            if (0 == Interlocked.CompareExchange(ref _isDisposed, 1, 0) && disposing)
            {
                _queue.Writer.Complete();
                if (!(_worker is null))
                {
                    try
                    {
                        if (!_worker.Wait(1000))
                        {
                            Console.Error.WriteLine("Logging worker did not stop withing 1 second.");
                        }
                    }
                    catch (Exception exn)
                    {
                        Console.Error.WriteLine("Logging worker exited with exception.");
                        Console.Error.WriteLine(exn);
                    }
                }
                _cancellation.Cancel();
                _sink.Dispose();
                _cancellation.Dispose();
            }
        }

        /// <summary>
        /// Only disposes managed resources.
        /// </summary>
        protected virtual async ValueTask DisposeAsyncCore()
        {
            if (0 == Interlocked.CompareExchange(ref _isDisposed, 1, 0))
            {
                _queue.Writer.Complete();
                if (!(_worker is null))
                {
                    await _worker.ConfigureAwait(false);
                }
                _cancellation.Cancel();
                await _sink.DisposeAsync().ConfigureAwait(false);
                _cancellation.Dispose();
            }
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

        private void EnsureWorker()
        {
            lock (_sync)
            {
                _worker ??= RunWorker(_cancellation.Token);
            }
        }

        protected virtual async Task RunWorker(CancellationToken cancellationToken)
        {
            try
            {
                var reader = _queue.Reader;
                var buffer = new LogMessage[20];
                // initially we wait for the first item to become available.
                while (await reader.WaitToReadAsync(cancellationToken).ConfigureAwait(false))
                {
                    while (reader.TryRead(out var message))
                    {
                        if (_sink is IBulkSink bulkSink)
                        {
                            // if the sink does support queueing --> try wait for further entries
                            buffer[0] = message;
                            var count = await reader.ReadAllAvailableWithinAsync(buffer, 1, TimeSpan.FromMilliseconds(500), cancellationToken).ConfigureAwait(false);
                            await using var queue = bulkSink.CreateQueue();
                            for (var i = 0; i < count; ++i)
                            {
                                buffer[i].Enqueue(queue);
                            }
                            await queue.FlushAsync(CancellationToken.None).ConfigureAwait(false);
                            #if NETSTANDARD2_1
                            Array.Fill(buffer, default);
                            #else
                            for (var i = 0; i < buffer.Length; ++i)
                            {
                                buffer[i] = default!;
                            }
                            #endif
                        }
                        else
                        {
                            // if the sink does not support queueing --> log it and keep waiting
                            await message.LogAsync(_sink, CancellationToken.None).ConfigureAwait(false);
                        }
                    }
                }
                Console.WriteLine("Logger worker has been completed normally.");
            }
            catch (OperationCanceledException)
            {
                Console.WriteLine("Logger worker has been cancelled.");
            }
        }

        protected virtual Logger DoCreateLogger(string categoryName)
            => new Logger(this, categoryName);

        public Logger CreateLogger(string categoryName)
        {
            EnsureWorker();
            return DoCreateLogger(categoryName);
        }

        public virtual void PushMessage(LogMessage message)
        {
            var task = _queue.Writer.WriteAsync(message, _cancellation.Token);
            if (!task.IsCompletedSuccessfully)
            {
                task.AsTask().Wait(_cancellation.Token);
            }
        }
    }
}
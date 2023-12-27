using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace NCoreUtils.Logging;

public partial class LoggerProvider(ISink sink) : ILoggerProvider, IDisposable, IAsyncDisposable
{
    private readonly object _sync = new();

    private readonly CancellationTokenSource _cancellation = new();

    private readonly ISink _sink = sink ?? throw new ArgumentNullException(nameof(sink));

    private int _isDisposed;

    private Task? _worker;

    ILogger ILoggerProvider.CreateLogger(string categoryName)
        => CreateLogger(categoryName);

    #region IDisposable

    protected virtual void Dispose(bool disposing)
    {
        // NOTE: no unmanaged disposables
        if (0 == Interlocked.CompareExchange(ref _isDisposed, 1, 0) && disposing)
        {
            CompleteQueue();
            if (_worker is not null)
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
            CompleteQueue();
            if (_worker is not null)
            {
                await _worker;
            }
            _cancellation.Cancel();
            await _sink.DisposeAsync();
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
            var reader = GetQueueReader();
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
                        for (var i = 0; i < count; ++i)
                        {
                            buffer[i].Dispose();
                            buffer[i] = default!;
                        }
                    }
                    else
                    {
                        // if the sink does not support queueing --> log it and keep waiting
                        await message.LogAsync(_sink, CancellationToken.None).ConfigureAwait(false);
                        message.Dispose();
                    }
                }
            }
            Console.WriteLine("Logger worker has been completed normally.");
        }
        catch (OperationCanceledException)
        {
            Console.WriteLine("Logger worker has been cancelled.");
        }
        catch (Exception exn)
        {
            Console.WriteLine($"Logger worker has exited due to exception: {exn}");
            throw;
        }
    }

    protected virtual Logger DoCreateLogger(string categoryName)
        => new(this, categoryName);

    public Logger CreateLogger(string categoryName)
    {
        EnsureWorker();
        return DoCreateLogger(categoryName);
    }

    public virtual void PushMessage(LogMessage message)
    {
        var task = PushToQueueAsync(message, _cancellation.Token);
        if (!task.IsCompletedSuccessfully)
        {
            task.AsTask().Wait(_cancellation.Token);
        }
    }
}
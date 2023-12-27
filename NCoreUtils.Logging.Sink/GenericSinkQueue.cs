using System;
using System.Buffers;
using System.Collections.Concurrent;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace NCoreUtils.Logging;

public class GenericSinkQueue<TPayload>(GenericBulkSink<TPayload> sink) : ISinkQueue
{
    private int _isDisposed;

    protected bool IsDisposed
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => 0 != Interlocked.CompareExchange(ref _isDisposed, 0, 0);
    }

    protected ConcurrentQueue<TPayload> Payloads { get; } = new ConcurrentQueue<TPayload>();

    protected GenericBulkSink<TPayload> Sink { get; } = sink ?? throw new ArgumentNullException(nameof(sink));

    #region IDisposable

    protected virtual void Dispose(bool disposing)
    {
        if (0 == Interlocked.CompareExchange(ref _isDisposed, 1, 0))
        {
            if (disposing)
            {
                try
                {
                    FlushAsync(default).AsTask().Wait();
                }
                catch (Exception exn)
                {
                    Console.Error.WriteLine("Failed to flush queue on disposal.");
                    Console.Error.WriteLine(exn);
                }
            }
        }
    }

    /// <summary>
    /// Only disposes managed resources.
    /// </summary>
    protected virtual async ValueTask DisposeAsyncCore()
    {
        if (0 == Interlocked.CompareExchange(ref _isDisposed, 1, 0))
        {
            await FlushAsync();
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

    public void Enqueue<TState>(LogMessage<TState> message)
    {
        if (IsDisposed)
        {
            throw new ObjectDisposedException(GetType().Name);
        }
        Payloads.Enqueue(Sink.CreatePayload(message));
    }

    public async ValueTask FlushAsync(CancellationToken cancellationToken = default)
    {
        var count = Payloads.Count;
        if (count > 0)
        {
            var package = ArrayPool<TPayload>.Shared.Rent(count);
            try
            {
#if NETFRAMEWORK
                for (var i = 0; i < count && Payloads.TryDequeue(out var item); ++i)
                {
                    package[i] = item;
                }
#else
                Payloads.CopyTo(package, 0);
                Payloads.Clear();
#endif
                await Sink.WritePayloadsAsync(package.Take(count), cancellationToken);
            }
            finally
            {
                ArrayPool<TPayload>.Shared.Return(package);
            }
        }
    }
}
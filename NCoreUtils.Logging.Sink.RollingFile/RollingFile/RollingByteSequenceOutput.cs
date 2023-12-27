using System;
using System.Threading;
using System.Threading.Tasks;
using IO = System.IO;

namespace NCoreUtils.Logging.RollingFile;

public class RollingByteSequenceOutput : IByteSequenceOutput
{
    private sealed class Target : IAsyncDisposable, IDisposable
    {
        public IO.Stream Stream { get; }

        public IFormattedPath Path { get; }

        public long Length => Stream.Length;

        public DateOnly? Timestamp => Path.Timestamp;

        public Target(IFormattedPath path)
        {
            Stream = new IO.FileStream(path.Path, IO.FileMode.Append, IO.FileAccess.Write, IO.FileShare.ReadWrite, 8 * 1024, true);
            Path = path;
        }

        public ValueTask DisposeAsync()
            => Stream.DisposeAsync();

        public void Dispose()
            => Stream.Dispose();
    }

    private static async void DisposeTarget(Target target)
    {
        try
        {
            await target.DisposeAsync().ConfigureAwait(false);
        }
        catch (Exception exn)
        {
            Console.Error.WriteLine("Failed to dispose rolling target.");
            Console.Error.WriteLine(exn);
        }
    }

    private InterlockedBoolean _isDiposed;

    private InterlockedBoolean _sync;

    private ref InterlockedBoolean Sync => ref _sync;

    private ref InterlockedBoolean IsDisposed => ref _isDiposed;

    private Target? CurrentTarget { get; set; }

    public IFileRoller Roller { get; }

    public FileNameDecomposition TargetPathDecomposition { get; }

    public IFileRollerOptions Options => Roller.Options;

    public RollingByteSequenceOutput(IFileRoller roller, string path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            throw new ArgumentException($"'{nameof(path)}' cannot be null or whitespace.", nameof(path));
        }
        Roller = roller ?? throw new ArgumentNullException(nameof(roller));
        TargetPathDecomposition = new FileNameDecomposition(path);
    }

    public async ValueTask WriteAsync(ReadOnlyMemory<byte> data, CancellationToken cancellationToken = default)
    {
        while (!Sync.TrySet()) { /* noop */ }
        try
        {
            if (CurrentTarget is null || Roller.ShouldRoll(TargetPathDecomposition, CurrentTarget.Timestamp, CurrentTarget.Length))
            {
                var lastTarget = CurrentTarget;
                if (lastTarget is not null)
                {
                    DisposeTarget(lastTarget);
                }
                var newPath = await Roller.RollAsync(TargetPathDecomposition, lastTarget?.Path, CancellationToken.None);
                CurrentTarget = new Target(newPath);
            }
        }
        finally
        {
            Sync.TryReset();
        }
        await CurrentTarget.Stream.WriteAsync(data, cancellationToken);
        await CurrentTarget.Stream.FlushAsync(CancellationToken.None);
    }

    #region disposable

    public void Dispose()
    {
        GC.SuppressFinalize(this);
        Dispose(disposing: true);
    }

    public async ValueTask DisposeAsync()
    {
        GC.SuppressFinalize(this);
        await DisposeAsyncCore();
        Dispose(disposing: false);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (disposing && IsDisposed.TrySet())
        {
            var target = CurrentTarget;
            CurrentTarget = default;
            target?.Dispose();
        }
    }

    protected virtual ValueTask DisposeAsyncCore()
    {
        if (IsDisposed.TrySet())
        {
            var target = CurrentTarget;
            CurrentTarget = default;
            return target?.DisposeAsync() ?? default;
        }
        return default;
    }

    #endregion
}
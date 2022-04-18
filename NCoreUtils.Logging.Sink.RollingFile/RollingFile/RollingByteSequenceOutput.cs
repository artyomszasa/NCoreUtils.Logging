using System;
using System.Threading;
using System.Threading.Tasks;
using IO = System.IO;

namespace NCoreUtils.Logging.RollingFile
{
    public class RollingByteSequenceOutput : IByteSequenceOutput
    {
        private sealed class Target : IAsyncDisposable, IDisposable
        {
            public IO.Stream Stream { get; }

            public IFormattedPath Path { get; }

            public long Length => Stream.Length;

            public DateTime? Timestamp => Path.Timestamp;

            public Target(IFormattedPath path)
            {
                Stream = new IO.FileStream(path.Path, IO.FileMode.CreateNew, IO.FileAccess.Write, IO.FileShare.ReadWrite, 8 * 1024, true);
                Path = path;
            }

            public ValueTask DisposeAsync()
                => Stream.DisposeAsync();

            public void Dispose()
                => Stream.Dispose();
        }

        private int _idDiposed;

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
            if (CurrentTarget is null || Roller.ShouldRoll(TargetPathDecomposition, CurrentTarget.Timestamp, CurrentTarget.Length))
            {
                CurrentTarget = new Target(await Roller.RollAsync(TargetPathDecomposition, CurrentTarget?.Path, CancellationToken.None));
            }
            await CurrentTarget.Stream.WriteAsync(data, cancellationToken);
        }

        #region disposable

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

        protected virtual void Dispose(bool disposing)
        {
            if (0 == Interlocked.CompareExchange(ref _idDiposed, 1, 0))
            {
                if (disposing)
                {
                    var target = CurrentTarget;
                    CurrentTarget = default;
                    target?.Dispose();
                }
            }
        }

        protected virtual ValueTask DisposeAsyncCore()
        {
            if (0 == Interlocked.CompareExchange(ref _idDiposed, 1, 0))
            {
                var target = CurrentTarget;
                CurrentTarget = default;
                return (target?.DisposeAsync() ?? default);
            }
            return default;
        }

        #endregion
    }
}
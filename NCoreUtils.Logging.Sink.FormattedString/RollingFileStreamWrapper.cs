using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace NCoreUtils.Logging.FormattedString
{
    public class RollingFileStreamWrapper : IOutputStreamWrapper
    {
        private sealed class StreamData : IAsyncDisposable, IDisposable
        {
            public FileStream Stream { get; }

            public DateTime Date { get; }

            public StreamData(FileStream stream, DateTime date)
            {
                Stream = stream ?? throw new ArgumentNullException(nameof(stream));
                Date = date;
            }

            public ValueTask DisposeAsync()
                => Stream.DisposeAsync();

            public void Dispose()
                => Stream.Dispose();
        }

        private struct Pattern
        {
            public string Base { get; }

            public string? Suffix { get; }

            public Pattern(string @base, string? suffix)
            {
                Base = @base ?? throw new ArgumentNullException(nameof(@base));
                Suffix = suffix;
            }
        }

        private static DateTime Today()
        {
            var now = DateTime.Now;
            return new DateTime(now.Year, now.Month, now.Day, 0, 0, 0, DateTimeKind.Unspecified);
        }

        private int _isDisposed;

        private Pattern Pat { get; }

        private StreamData? Output { get; set; }

        public RollingFileStreamWrapper(string path)
        {
            var dotIndex = path.LastIndexOf('.');
            if (dotIndex == -1)
            {
                Pat = new Pattern(path, default);
            }
            else
            {
                var sepIndex = path.LastIndexOf(Path.DirectorySeparatorChar);
                if (sepIndex == -1 || sepIndex < dotIndex)
                {
                    Pat = new Pattern(path[..dotIndex], path[dotIndex..]);
                }
                else
                {
                    Pat = new Pattern(path, default);
                }
            }
        }

        public async ValueTask WriteAsync(ReadOnlyMemory<byte> data, CancellationToken cancellationToken = default)
        {
            var today = Today();
            if (Output is null || Output.Date != today)
            {
                await (Output?.DisposeAsync() ?? default);
                var path = $"{Pat.Base}.{today:yyyy-MM-dd}{Pat.Suffix}";
                Output = new StreamData(
                    stream: new FileStream(path, FileMode.Append, FileAccess.Write, FileShare.Read, 16 * 1024, true),
                    date: today
                );
            }
            await Output.Stream.WriteAsync(data, cancellationToken).ConfigureAwait(false);;
            await Output.Stream.FlushAsync(cancellationToken).ConfigureAwait(false);
        }

        #region Disposable

        protected virtual void Dispose(bool disposing)
        {
            if (0 == Interlocked.CompareExchange(ref _isDisposed, 1, 0))
            {
                if (disposing)
                {
                    Output?.Dispose();
                }
            }
        }

        protected virtual ValueTask DisposeAsyncCore()
        {
            if (0 == Interlocked.CompareExchange(ref _isDisposed, 1, 0))
            {
                return Output?.DisposeAsync() ?? default;
            }
            return default;
        }

        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        public async ValueTask DisposeAsync()
        {
            await DisposeAsyncCore().ConfigureAwait(false);
            Dispose(disposing: false);
#pragma warning disable CA1816 // Dispose methods should call SuppressFinalize
            GC.SuppressFinalize(this);
#pragma warning restore CA1816 // Dispose methods should call SuppressFinalize
        }

        #endregion
    }
}
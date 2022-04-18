using System;
using System.Threading;
using System.Threading.Tasks;
using IO = System.IO;

namespace NCoreUtils.Logging.RollingFile
{
    public class DefaultFileRoller : IFileRoller
    {
        private const int BufferSize = 32 * 1024;

        private static DateTime Today()
        {
            var now = DateTime.Now;
            return new DateTime(now.Year, now.Month, now.Day, 0, 0, 0, DateTimeKind.Unspecified);
        }

        private static IO.FileStream OpenRead(string path)
            => new(
                path,
                IO.FileMode.Open,
                IO.FileAccess.Read,
                IO.FileShare.None,
                BufferSize,
                IO.FileOptions.SequentialScan | IO.FileOptions.Asynchronous
            );

        private static IO.FileStream OpenWrite(string path)
            => new(
                path,
                IO.FileMode.CreateNew,
                IO.FileAccess.Write,
                IO.FileShare.None,
                BufferSize,
                true
            );

        public IFileRollerOptions Options { get; }

        public DefaultFileRoller(IFileRollerOptions? options = default)
            => Options = options ?? new DefaultFileRollerOptions();

        private async ValueTask DoRollAsync(IFormattedPath path, CancellationToken cancellationToken)
        {
            (IFormattedPath FormattedPath, bool Compressed, string Path) pathToRoll;
            if (!IO.File.Exists(path.Path))
            {
                var compressedPath = path.Path + ".gz";
                if (!IO.File.Exists(compressedPath))
                {
                    return;
                }
                pathToRoll = (path, true, compressedPath);
            }
            else
            {
                pathToRoll = (path, false, path.Path);
            }
            var targetPath = pathToRoll.FormattedPath.WithSuffix(pathToRoll.FormattedPath.Suffix + 1);
            await DoRollAsync(targetPath, cancellationToken);
            if (!pathToRoll.Compressed && Options.CompressRolled)
            {
                var target = targetPath.Path + ".gz";
                await using var sourceStream = OpenRead(pathToRoll.Path);
                await using var targetStream = OpenWrite(target);
                var compressionLevel =
#if NETSTANDARD2_1
                    IO.Compression.CompressionLevel.Optimal;
#else
                    IO.Compression.CompressionLevel.SmallestSize;
#endif
                await using var gzStream = new IO.Compression.GZipStream(targetStream, compressionLevel);
            }
            else
            {
                var target = pathToRoll.Compressed ? targetPath.Path + ".gz" : targetPath.Path;
                await using var sourceStream = OpenRead(pathToRoll.Path);
                await using var targetStream = OpenWrite(target);
                await sourceStream.CopyToAsync(targetStream, BufferSize, cancellationToken);
            }
            IO.File.Delete(pathToRoll.Path);
        }

        public bool ShouldRoll(FileNameDecomposition basePath, DateTime? timestamp, long size)
        {
            if (timestamp.HasValue && Options.Triggers.HasFlag(FileRollTrigger.Date))
            {
                if (Today() != timestamp.Value.Date)
                {
                    return true;
                }
            }
            if (Options.Triggers.HasFlag(FileRollTrigger.Size) && Options.MaxFileSize > 0L)
            {
                if (Options.MaxFileSize >= size)
                {
                    return true;
                }
            }
            return false;
        }

        public async ValueTask<IFormattedPath> RollAsync(FileNameDecomposition basePath, IFormattedPath? lastPath, CancellationToken cancellationToken = default)
        {
            // if last path is provided it must be rolled withput condition
            if (lastPath is not null)
            {
                await DoRollAsync(lastPath, cancellationToken);
            }
            // initializing
            var now = DateTime.SpecifyKind(DateTime.Now, DateTimeKind.Unspecified);
            var candidate = Options.FileNameFormatter(basePath, now, 0);
            var finfo = new System.IO.FileInfo(candidate.Path);
            if (finfo.Exists && Options.Triggers.HasFlag(FileRollTrigger.Size) && Options.MaxFileSize > 0 && finfo.Length < Options.MaxFileSize)
            {
                // if file exists and size restrictions are provided and size restrictions are not met --> roll file
                await DoRollAsync(candidate, cancellationToken);
            }
            return candidate;
        }
    }
}
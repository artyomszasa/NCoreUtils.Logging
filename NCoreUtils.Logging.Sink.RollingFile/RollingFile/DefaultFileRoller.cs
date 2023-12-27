using System;
using System.Threading;
using System.Threading.Tasks;
using IO = System.IO;

namespace NCoreUtils.Logging.RollingFile;

public class DefaultFileRoller : IFileRoller
{
    private const int BufferSize = 32 * 1024;

    private const IO.Compression.CompressionLevel CompressionLevel =
#if NETSTANDARD2_1 || NETFRAMEWORK
        IO.Compression.CompressionLevel.Optimal;
#else
        IO.Compression.CompressionLevel.SmallestSize;
#endif

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

    private static async Task CompressAsync(string sourcePath, string targetPath, CancellationToken cancellationToken)
    {
#if !NETFRAMEWORK
        await
#endif
        using var sourceStream = OpenRead(sourcePath);
#if !NETFRAMEWORK
        await
#endif
        using var targetStream = OpenWrite(targetPath);
#if !NETFRAMEWORK
        await
#endif
        using var gzStream = new IO.Compression.GZipStream(targetStream, CompressionLevel);
        await sourceStream.CopyToAsync(gzStream, BufferSize, cancellationToken).ConfigureAwait(false);
    }

    public IDateProvider DateProvider { get; }

    public IFileRollerOptions Options { get; }

    public DefaultFileRoller(IDateProvider dateProvider, IFileRollerOptions? options = default)
    {
        DateProvider = dateProvider ?? throw new ArgumentNullException(nameof(dateProvider));
        Options = options ?? new DefaultFileRollerOptions();
    }

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
        if (Options.MaxFileSize > 0L)
        {
            // rolling suffixed files
            try
            {
                var targetPath = pathToRoll.FormattedPath.WithSuffix(pathToRoll.FormattedPath.Suffix + 1);
                await DoRollAsync(targetPath, cancellationToken);
                if (!pathToRoll.Compressed && Options.CompressRolled)
                {
                    var target = targetPath.Path + ".gz";
                    await CompressAsync(pathToRoll.Path, target, CancellationToken.None).ConfigureAwait(false);
                }
                else
                {
                    var target = pathToRoll.Compressed ? targetPath.Path + ".gz" : targetPath.Path;
#if !NETFRAMEWORK
                    await
#endif
                    using var sourceStream = OpenRead(pathToRoll.Path);
#if !NETFRAMEWORK
                    await
#endif
                    using var targetStream = OpenWrite(target);
                    await sourceStream.CopyToAsync(targetStream, BufferSize, cancellationToken);
                }
                IO.File.Delete(pathToRoll.Path);
            }
            catch (Exception exn)
            {
                Console.Error.WriteLine($"Failed to roll \"{pathToRoll.Path}\".");
                Console.Error.WriteLine(exn);
            }
        }
        else
        {
            // rolling by date only
            if (!pathToRoll.Compressed && Options.CompressRolled)
            {
                try
                {
                    var targetPath = pathToRoll.Path + ".gz";
                    await CompressAsync(pathToRoll.Path, targetPath, CancellationToken.None).ConfigureAwait(false);
                    IO.File.Delete(pathToRoll.Path);
                }
                catch (Exception exn)
                {
                    Console.Error.WriteLine($"Failed to compress \"{pathToRoll.Path}\".");
                    Console.Error.WriteLine(exn);
                }
            }
        }
    }

    public bool ShouldRoll(FileNameDecomposition basePath, DateOnly? date, long size)
    {
        if (date is DateOnly dateValue && Options.Triggers.HasFlag(FileRollTrigger.Date))
        {
            if (DateProvider.CurrentDate != dateValue)
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
        // if last path is provided it must be rolled without condition
        if (lastPath is not null)
        {
            await DoRollAsync(lastPath, cancellationToken);
        }
        // initializing
        var candidate = Options.FileNameFormatter(basePath, DateProvider.CurrentDate, 0);
        var finfo = new IO.FileInfo(candidate.Path);
        if (finfo.Exists && Options.Triggers.HasFlag(FileRollTrigger.Size) && Options.MaxFileSize > 0 && finfo.Length < Options.MaxFileSize)
        {
            // if file exists and size restrictions are provided and size restrictions are not met --> roll file
            await DoRollAsync(candidate, cancellationToken);
        }
        return candidate;
    }
}
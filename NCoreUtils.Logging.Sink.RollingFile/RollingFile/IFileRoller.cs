using System;
using System.Threading;
using System.Threading.Tasks;

namespace NCoreUtils.Logging.RollingFile
{
    public interface IFileRoller
    {
        IFileRollerOptions Options { get; }

        bool ShouldRoll(FileNameDecomposition basePath, DateOnly? timestamp, long size);

        ValueTask<IFormattedPath> RollAsync(FileNameDecomposition basePath, IFormattedPath? lastPath, CancellationToken cancellationToken = default);
    }
}
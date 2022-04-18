using System;

namespace NCoreUtils.Logging.RollingFile
{
    public class DefaultFileRollerOptions : IFileRollerOptions
    {
        public FileRollTrigger Triggers { get; set; } = FileRollTrigger.Date;

        public long MaxFileSize { get; set; } = 0;

        public bool CompressRolled { get; set; } = true;

        public FileNameFormatterDelegate FileNameFormatter { get; set; }
            = (in FileNameDecomposition fileName, DateTime timestamp, int suffix)
                => new DefaultFormattedPath(
                    "{0}.{2:yyyy-MM-dd}{1}",
                    "{0}.{2:yyyy-MM-dd}{1}.{3}",
                    in fileName,
                    timestamp,
                    suffix
                );
    }
}
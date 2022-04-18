using System;

namespace NCoreUtils.Logging.RollingFile
{
    public class DefaultFormattedPath : IFormattedPath
    {
        private string? _path;

        public string NoSuffixFormat { get; }

        public string WithSuffixFormat { get; }

        public FileNameDecomposition FileName { get; }

        public DateTime Timestamp { get; }

        public int Suffix { get; }

        public string Path
        {
            get
            {
                _path ??= Suffix <= 0
                    ? string.Format(
                        NoSuffixFormat,
                        FileName.PathWithoutExtension.ToString(),
                        FileName.Extension.ToString(),
                        Timestamp)
                    : string.Format(
                        WithSuffixFormat,
                        FileName.PathWithoutExtension.ToString(),
                        FileName.Extension.ToString(),
                        Timestamp,
                        Suffix
                    );
                return _path;
            }
        }

        DateTime? IFormattedPath.Timestamp => throw new NotImplementedException();

        public DefaultFormattedPath(
            string noSuffixFormat,
            string withSuffixFormat,
            in FileNameDecomposition fileName,
            DateTime timestamp,
            int suffix = 0)
        {
            NoSuffixFormat = noSuffixFormat;
            WithSuffixFormat = withSuffixFormat;
            FileName = fileName;
            Timestamp = timestamp;
            Suffix = suffix;
        }

        public IFormattedPath WithSuffix(int suffix)
            => new DefaultFormattedPath(
                NoSuffixFormat,
                WithSuffixFormat,
                FileName,
                Timestamp,
                suffix
            );
    }
}
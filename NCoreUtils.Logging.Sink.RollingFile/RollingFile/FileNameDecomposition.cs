using System;

namespace NCoreUtils.Logging.RollingFile
{
    public class FileNameDecomposition
    {
        private int DotIndex { get; }

        public string Path { get; }

        public ReadOnlySpan<char> PathWithoutExtension
            => DotIndex < 0
                ? Path.AsSpan()
                : Path.AsSpan()[..DotIndex];

        public ReadOnlySpan<char> Extension
            => DotIndex < 0
                ? string.Empty.AsSpan()
                : Path.AsSpan()[DotIndex..];

        public FileNameDecomposition(string path)
        {
            if (path is null)
            {
                throw new ArgumentNullException(nameof(path));
            }
            Path = path;
            var lastDotIndex = path.LastIndexOf('.');
            if (-1 == lastDotIndex)
            {
                DotIndex = -1;
            }
            else
            {
                var lastSepratorIndex = path.LastIndexOf(System.IO.Path.DirectorySeparatorChar);
                DotIndex = lastSepratorIndex == -1 || lastSepratorIndex < lastDotIndex
                    ? lastDotIndex
                    : -1;
            }
        }
    }
}
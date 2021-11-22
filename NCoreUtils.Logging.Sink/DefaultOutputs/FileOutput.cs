using System.IO;

namespace NCoreUtils.Logging.DefaultOutputs
{
    public class FileOutput : StreamOutput
    {
        public string Path { get; }

        public FileShare Share { get; }

        public bool Append { get; }

        public FileOutput(string path, FileShare share = FileShare.ReadWrite, bool append = true)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                throw new System.ArgumentException($"'{nameof(path)}' cannot be null or whitespace.", nameof(path));
            }
            Path = path;
            Share = share;
            Append = append;
        }

        protected override Stream InitializeStream()
            => new FileStream(
                Path,
                Append ? FileMode.Append : FileMode.Create,
                FileAccess.Write,
                Share,
                16 * 1024,
                true
            );
    }
}
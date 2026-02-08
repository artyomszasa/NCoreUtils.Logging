namespace NCoreUtils.Logging.DefaultOutputs;

public class FileOutput(string path, FileShare share = FileShare.ReadWrite, bool append = true)
    : StreamOutput
{
    public string Path { get; } = string.IsNullOrWhiteSpace(path)
            ? throw new ArgumentException($"'{nameof(path)}' cannot be null or whitespace.", nameof(path))
            : path;

    public FileShare Share { get; } = share;

    public bool Append { get; } = append;

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
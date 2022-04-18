namespace NCoreUtils.Logging.RollingFile
{
    public interface IFileRollerOptions
    {
        FileRollTrigger Triggers { get; }

        long MaxFileSize { get; }

        bool CompressRolled { get; }

        FileNameFormatterDelegate FileNameFormatter { get; }
    }
}
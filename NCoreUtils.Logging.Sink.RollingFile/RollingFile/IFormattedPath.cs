using System;

namespace NCoreUtils.Logging.RollingFile
{
    public interface IFormattedPath
    {
        string Path { get; }

        DateOnly? Timestamp { get; }

        int Suffix { get; }

        IFormattedPath WithSuffix(int suffix);
    }
}
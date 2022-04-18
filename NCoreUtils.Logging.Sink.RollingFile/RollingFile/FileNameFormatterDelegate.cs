using System;

namespace NCoreUtils.Logging.RollingFile
{
    public delegate IFormattedPath FileNameFormatterDelegate(in FileNameDecomposition path, DateTime timestamp, int suffix);
}
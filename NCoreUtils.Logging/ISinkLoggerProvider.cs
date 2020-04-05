using Microsoft.Extensions.Logging;

namespace NCoreUtils.Logging
{
    public interface ISinkLoggerProvider<TSink> : ILoggerProvider
        where TSink : ISink
    { }
}
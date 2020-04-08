using System;
using System.Threading;
using System.Threading.Tasks;

namespace NCoreUtils.Logging.Google.Fluentd
{
    public interface IFluentdSink : IAsyncDisposable
    {
        ValueTask WriteAsync(string entry, CancellationToken cancellationToken);
    }
}
using System.Threading;
using System.Threading.Tasks;

namespace NCoreUtils.Logging
{
    public interface IAspNetCoreSink : ISink
    {
        ValueTask LogAsync<TState>(AspNetCoreLogMessage<TState> message, CancellationToken cancellationToken = default);
    }
}
using System;
using Microsoft.Extensions.Logging;

namespace NCoreUtils.Logging
{
    public interface IAspNetCoreSinkQueue : ISinkQueue
    {
        void Enqueue<TState>(AspNetCoreLogMessage<TState> message);
    }
}
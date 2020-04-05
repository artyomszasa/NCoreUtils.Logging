using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace NCoreUtils.Logging
{
    public class AspNetCoreLogMessage<TState> : LogMessage<TState>
    {
        public AspNetCoreContext Context { get; }

        public bool IsRequestSummary { get; }

        public AspNetCoreLogMessage(
            string category,
            LogLevel logLevel,
            EventId eventId,
            Exception? exception,
            TState state,
            Func<TState, Exception?, string> formatter,
            AspNetCoreContext context,
            bool isRequestSummary)
            : base(category, logLevel, eventId, exception, state, formatter)
        {
            Context = context;
            IsRequestSummary = isRequestSummary;
        }

        public override ValueTask LogAsync(ISink sink, CancellationToken cancellationToken = default)
            => sink switch
            {
                IAspNetCoreSink aspSink => aspSink.LogAsync(this),
                _ => base.LogAsync(sink, cancellationToken)
            };

        public override void Enqueue(ISinkQueue queue)
        {
            if (queue is IAspNetCoreSinkQueue aspQueue)
            {
                aspQueue.Enqueue(this);
            }
            else
            {
                base.Enqueue(queue);
            }
        }
    }
}
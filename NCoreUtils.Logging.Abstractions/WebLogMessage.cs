using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace NCoreUtils.Logging
{
    public class WebLogMessage<TState> : LogMessage<TState>
    {
        public WebContext Context { get; }

        public bool IsRequestSummary { get; }

        public WebLogMessage(
            string category,
            LogLevel logLevel,
            EventId eventId,
            Exception? exception,
            TState state,
            Func<TState, Exception?, string> formatter,
            in WebContext context,
            bool isRequestSummary)
            : base(category, logLevel, eventId, exception, state, formatter)
        {
            Context = context;
            IsRequestSummary = isRequestSummary;
        }
    }
}
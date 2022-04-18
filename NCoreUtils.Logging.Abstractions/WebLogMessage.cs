using System;
using Microsoft.Extensions.Logging;
using NCoreUtils.Logging.Internal;

namespace NCoreUtils.Logging
{
    public class WebLogMessage : LogMessage<string>
    {
        private static FixSizePool<WebLogMessage> Pool { get; } = new(8 * 1024);

        public static WebLogMessage Initialize(
            string category,
            LogLevel logLevel,
            EventId eventId,
            Exception? exception,
            string state,
            Func<string, Exception?, string> formatter,
            in WebContext context,
            bool isRequestSummary)
            => Pool.TryRent(out var msg)
                ? msg.Update(category, logLevel, eventId, exception, state, formatter, in context, isRequestSummary)
                : new(category, logLevel, eventId, exception, state, formatter, in context, isRequestSummary);

        private static int _cc;

        private static int _up;

        private WebContext _context;

        public ref WebContext Context => ref _context;

        public bool IsRequestSummary { get; private set; }

        private WebLogMessage(
            string category,
            LogLevel logLevel,
            EventId eventId,
            Exception? exception,
            string state,
            Func<string, Exception?, string> formatter,
            in WebContext context,
            bool isRequestSummary)
            : base(category, logLevel, eventId, exception, state, formatter)
        {
            System.Threading.Interlocked.Increment(ref _cc);
            _context = context;
            IsRequestSummary = isRequestSummary;
        }

        protected WebLogMessage Update(
            string category,
            LogLevel logLevel,
            EventId eventId,
            Exception? exception,
            string state,
            Func<string, Exception?, string> formatter,
            in WebContext context,
            bool isRequestSummary)
        {
            System.Threading.Interlocked.Increment(ref _up);
            base.Update(category, logLevel, eventId, exception, state, formatter);
            _context = context;
            IsRequestSummary = isRequestSummary;
            return this;
        }

        public override void Dispose()
        {
            System.Threading.Interlocked.Decrement(ref _up);
            Pool.Return(this);
        }
    }
}
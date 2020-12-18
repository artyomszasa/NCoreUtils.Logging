using System;
using System.Collections.Generic;
using System.Globalization;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace NCoreUtils.Logging
{
    public class AspNetCoreLogger : Logger
    {
        private ref struct HostingRequestFinishedLogWrapper
        {
            readonly IReadOnlyList<KeyValuePair<string, object>> _source;

            public HostingRequestFinishedLogWrapper(object source)
                => _source = (IReadOnlyList<KeyValuePair<string, object>>)source;

            public void Apply(ref WebContext context)
            {
                foreach (var kv in _source)
                {
                    switch(kv.Key)
                    {
                        case "ElapsedMilliseconds":
                            context.Latency = TimeSpan.FromMilliseconds(((double)kv.Value));
                            break;
                        case nameof(HttpResponse.StatusCode):
                            context.ResponseStatusCode = (int)kv.Value;
                            break;
                        case nameof(HttpResponse.ContentType):
                            context.ResponseContentType = (string?)kv.Value;
                            break;
                        case nameof(HttpResponse.ContentLength):
                            context.ResponseContentLength = (long?)kv.Value;
                            break;
                        default:
                            break;
                    }
                }
            }
        }

        private static readonly Func<string, Exception?, string> _passString = (s, _) => s;

        private static TService? GetServiceSafe<TService>(IServiceProvider? serviceProvider)
            where TService : class
        {
            if (serviceProvider is null)
            {
                return default;
            }
            return serviceProvider.GetService(typeof(TService)) as TService;
        }

        private readonly IHttpContextAccessor _httpContextAccessor;

        public AspNetCoreLogger(LoggerProvider provider, string categoryName, IHttpContextAccessor httpContextAccessor)
            : base(provider, categoryName)
            => _httpContextAccessor = httpContextAccessor ?? throw new ArgumentNullException(nameof(httpContextAccessor));

        private WebContext GetCurrentAspNetCoreContext()
        {
            try
            {
                var httpContext = _httpContextAccessor.HttpContext;
                if (httpContext is null)
                {
                    return default;
                }
                var loggingContext = GetServiceSafe<LoggingContext>(httpContext.RequestServices);
                if (loggingContext is null)
                {
                    lock (httpContext)
                    {
                        var ctx = new WebContext();
                        LoggingContext.PopulateContext(ref ctx, httpContext);
                        return ctx;
                    }
                }
                // Current user and response code may have changed during the execution, try update
                try
                {
                    loggingContext.UpdateFrom(httpContext);
                }
                catch { }
                return loggingContext.WebContext;
            }
            catch (Exception exn)
            {
                Console.Error.WriteLine("Unable to get logging context.");
                Console.Error.WriteLine(exn);
                return default;
            }
        }

        public override void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            // NOTE: State may contain non-threadsafe references (e.g. Microsoft.AspNetCore.Hosting.Internal.HostingRequestStartingLog -> HttpContext)
            // thus formatter should be executed before passing to the delivery thread
            string evaluatedState;
            // if this is request finish log
            if (state != null && state.GetType().FullName == "Microsoft.AspNetCore.Hosting.HostingRequestFinishedLog")
            {
                // if this is internal request finish log, then override pre-populated values.
                var ctx = GetCurrentAspNetCoreContext();
                new HostingRequestFinishedLogWrapper(state).Apply(ref ctx);
                evaluatedState = formatter(state, exception);
                Provider.PushMessage(new WebLogMessage<string>(CategoryName, logLevel, eventId, exception, evaluatedState, _passString, ctx, true));
            }
            else
            {
                evaluatedState = formatter(state, exception);
                Provider.PushMessage(new WebLogMessage<string>(CategoryName, logLevel, eventId, exception, evaluatedState, _passString, GetCurrentAspNetCoreContext(), false));
            }
        }
    }
}
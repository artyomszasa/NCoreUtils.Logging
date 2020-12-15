using System;
using Microsoft.AspNetCore.Http;

namespace NCoreUtils.Logging.Google
{
    public class DefaultTraceIdProvider : ITraceIdProvider
    {
        private static string NextId() => Guid.NewGuid().ToString("N");

        private readonly IHttpContextAccessor? _httpContextAccessor;

        public DefaultTraceIdProvider(IHttpContextAccessor? httpContextAccessor = default)
            => _httpContextAccessor = httpContextAccessor;

        public string TraceId
        {
            get
            {
                var httpContext = _httpContextAccessor?.HttpContext;
                if (httpContext is null)
                {
                    Console.Error.WriteLine("Default trace id provider is used out-of aspt net core request context.");
                    return NextId();
                }
                if (httpContext.Items.TryGetValue(HttpContextItemIds.TraceId, out var boxedId))
                {
                    return boxedId as string ?? NextId();
                }
                var traceId = httpContext.Request.Headers.TryGetValue("X-Trace-Id", out var values) && values.Count > 0
                    ? values[0]
                    : NextId();
                httpContext.Items[HttpContextItemIds.TraceId] = traceId;
                return traceId;
            }
        }
    }
}
using System;
using Microsoft.AspNetCore.Http;

namespace NCoreUtils.Logging
{
    public class DefaultTraceIdProvider : ITraceIdProvider
    {
        private static string NextId() => Guid.NewGuid().ToString("N");

        private readonly IHttpContextAccessor? _httpContextAccessor;

        public DefaultTraceIdProvider(IHttpContextAccessor? httpContextAccessor = default)
            => _httpContextAccessor = httpContextAccessor;

        public bool SuppressOutOfContextWarning { get; set; }

        public string TraceId
        {
            get
            {
                var httpContext = _httpContextAccessor?.HttpContext;
                if (httpContext is null)
                {
                    if (!SuppressOutOfContextWarning)
                    {
                        Console.Error.WriteLine("Default trace id provider is used out-of ASP.NET Core request context.");
                    }
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
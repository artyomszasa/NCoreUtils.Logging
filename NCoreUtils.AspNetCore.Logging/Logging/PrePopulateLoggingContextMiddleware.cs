using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace NCoreUtils.Logging
{
    public class PrePopulateLoggingContextMiddleware
    {
        readonly RequestDelegate _next;

        public PrePopulateLoggingContextMiddleware(RequestDelegate next)
            => _next = next;

        public Task InvokeAsync(HttpContext httpContext, LoggingContext? loggingContext = default)
        {
            if (loggingContext is null)
            {
                throw new InvalidOperationException("No logging context found. Add logging context using services.AddLoggingContext().");
            }
            loggingContext.PopulateFrom(httpContext);
            return _next(httpContext);
        }
    }
}
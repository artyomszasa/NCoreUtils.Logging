using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Hosting;

namespace NCoreUtils.Logging
{
    public class PrePopulateLoggingContextMiddleware
    {

        private readonly RequestDelegate _next;

        private readonly IWebHostEnvironment _env;

        public PrePopulateLoggingContextMiddleware(RequestDelegate next, IWebHostEnvironment env)
        {
            _next = next ?? throw new ArgumentNullException(nameof(next));
            _env = env ?? throw new ArgumentNullException(nameof(env));
        }

        public Task InvokeAsync(HttpContext httpContext, LoggingContext? loggingContext = default)
        {
            if (loggingContext is null)
            {
                // allow execution without LoggingContext when environment is DEVELOPMENT.
                if (_env.IsDevelopment())
                {
                    return _next(httpContext);
                }
                throw new InvalidOperationException("No logging context found. Add logging context using services.AddLoggingContext().");
            }
            loggingContext.PopulateFrom(httpContext);
            return _next(httpContext);
        }
    }
}
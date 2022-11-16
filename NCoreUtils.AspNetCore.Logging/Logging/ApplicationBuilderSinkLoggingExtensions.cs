using System;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace NCoreUtils.Logging
{
    public static class ApplicationBuilderSinkLoggingExtensions
    {
        public static IApplicationBuilder UsePrePopulateLoggingContext(this IApplicationBuilder app)
        {
            var env = app.ApplicationServices.GetRequiredService<IWebHostEnvironment>();
            var isDevelopment = env.IsDevelopment();
            return app.Use((httpContext, next) =>
            {
                var loggingContext = httpContext.RequestServices.GetService<LoggingContext>();
                if (loggingContext is null)
                {
                    // allow execution without LoggingContext when environment is DEVELOPMENT.
                    if (isDevelopment)
                    {
#if NET6_0_OR_GREATER
                        return next(httpContext);
#else
                        return next();
#endif
                    }
                    throw new InvalidOperationException("No logging context found. Add logging context using services.AddLoggingContext().");
                }
                loggingContext.PopulateFrom(httpContext);
#if NET6_0_OR_GREATER
                return next(httpContext);
#else
                return next();
#endif
            });
        }
    }
}
using Microsoft.AspNetCore.Builder;
using NCoreUtils.Logging;

namespace NCoreUtils.AspNetCore
{
    public static class ApplicationBuilderSinkLoggingExtensions
    {
        public static IApplicationBuilder UsePrePopulateLoggingContext(this IApplicationBuilder app)
            => app.UseMiddleware<PrePopulateLoggingContextMiddleware>();
    }
}
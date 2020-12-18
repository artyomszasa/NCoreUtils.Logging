using Microsoft.AspNetCore.Builder;

namespace NCoreUtils.Logging
{
    public static class ApplicationBuilderSinkLoggingExtensions
    {
        public static IApplicationBuilder UsePrePopulateLoggingContext(this IApplicationBuilder app)
            => app.UseMiddleware<PrePopulateLoggingContextMiddleware>();
    }
}
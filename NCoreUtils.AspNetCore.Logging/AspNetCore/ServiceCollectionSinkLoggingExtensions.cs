using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using NCoreUtils.Logging;

namespace NCoreUtils.AspNetCore
{
    public static class ServiceCollectionSinkLoggingExtensions
    {
        public static IServiceCollection AddLoggingContext(this IServiceCollection services)
        {
            services.TryAddScoped<LoggingContext>();
            return services;
        }
    }
}
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace NCoreUtils.Logging
{
    public static class ServiceCollectionSinkLoggingExtensions
    {
        public static IServiceCollection AddLoggingContext(this IServiceCollection services)
        {
            services.TryAddScoped<LoggingContext>();
            return services;
        }

        public static IServiceCollection AddDefaultTraceIdProvider(this IServiceCollection services)
        {
            services.TryAddSingleton<ITraceIdProvider, DefaultTraceIdProvider>();
            return services;
        }
    }
}
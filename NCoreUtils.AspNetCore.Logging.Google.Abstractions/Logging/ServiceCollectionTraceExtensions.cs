using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using NCoreUtils.Logging.Google;

namespace NCoreUtils.Logging
{
    public static class ServiceCollectionTraceExtensions
    {
        public static IServiceCollection AddDefaultGoogleTraceIdProvider(this IServiceCollection services)
        {
            services.TryAddSingleton<ITraceIdProvider, DefaultTraceIdProvider>();
            return services;
        }
    }
}
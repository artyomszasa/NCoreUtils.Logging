using System;
using System.Diagnostics.CodeAnalysis;

namespace NCoreUtils.Logging
{
    internal static class ServiceProviderExtensions
    {
        public static bool TryGetOptionalService<T>(this IServiceProvider serviceProvider, [NotNullWhen(true)] out T? service)
            where T : class
        {
            if (serviceProvider is null)
            {
                service = default;
                return false;
            }
            var svc = serviceProvider.GetService(typeof(T));
            if (!(svc is null))
            {
                service = (T)svc;
                return true;
            }
            service = default;
            return false;
        }
    }
}
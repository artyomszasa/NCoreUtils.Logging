using System;

namespace NCoreUtils.Logging.Google.Internal
{
    public static class ServiceProviderGoogleLoggingConfigurationExtensions
    {
        private sealed class OverriddenServiceProvider<TService> : IServiceProvider
            where TService : notnull
        {
            private readonly IServiceProvider _parent;

            private readonly TService _service;

            public OverriddenServiceProvider(IServiceProvider parent, TService service)
            {
                _parent = parent ?? throw new ArgumentNullException(nameof(parent));
                _service = service;
            }

            public object GetService(Type serviceType)
            {
                if (serviceType == typeof(IServiceProvider))
                {
                    return this;
                }
                if (serviceType == typeof(TService))
                {
                    return _service;
                }
                return _parent.GetService(serviceType);
            }
        }

        public static IServiceProvider Override<TService>(this IServiceProvider serviceProvider, TService service)
            where TService : notnull
            => new OverriddenServiceProvider<TService>(serviceProvider, service);
    }
}
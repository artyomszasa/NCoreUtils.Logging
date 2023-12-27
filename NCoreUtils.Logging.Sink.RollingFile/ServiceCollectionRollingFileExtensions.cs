using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.DependencyInjection;
using NCoreUtils.Logging.RollingFile;

namespace NCoreUtils.Logging
{
    public static class ServiceCollectionRollingFileExtensions
    {
        public static IServiceCollection AddRollingFileLogging<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] TFileRoller>(
            this IServiceCollection services,
            ServiceLifetime lifetime = ServiceLifetime.Transient)
            where TFileRoller : IFileRoller
        {
            services.Add(ServiceDescriptor.Describe(typeof(IFileRoller), typeof(TFileRoller), lifetime));
            return services;
        }

        public static IServiceCollection AddRollingFileLogging(this IServiceCollection services, IFileRollerOptions options)
            => services
                .AddSingleton(options)
                .AddRollingFileLogging<DefaultFileRoller>();

        public static IServiceCollection AddRollingFileLogging(this IServiceCollection services)
            => services
                .AddSingleton<IByteSequenceOutputFactory, RollingByteSequenceOutputFactory>();
    }
}
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NCoreUtils.Logging.FormattedString;

namespace NCoreUtils.Logging
{
    public static class LoggingBuilderFormattedStringExtensions
    {
        public interface IOutputStreamWrappers
        {
            IOutputStreamWrapper Create(IServiceProvider servieProvider, string name);
        }

        private sealed class OutputStreamWrappers : IOutputStreamWrappers
        {
            private Dictionary<string, Func<IServiceProvider, IOutputStreamWrapper>> Factories { get; }
                = new Dictionary<string, Func<IServiceProvider, IOutputStreamWrapper>>();

            public void Add(string name, Func<IServiceProvider, IOutputStreamWrapper> factory)
                => Factories.Add(name, factory);

            public IOutputStreamWrapper Create(IServiceProvider servieProvider, string name)
                => Factories[name](servieProvider);
        }

        private static int _idSupply = 0;

        private static OutputStreamWrappers GetOrAddOutputStreamWrappers(this IServiceCollection services)
        {
            var descriptor = services.FirstOrDefault(desc => desc.ServiceType == typeof(IOutputStreamWrappers));
            if (descriptor is null)
            {
                var instance = new OutputStreamWrappers();
                services.AddSingleton<IOutputStreamWrappers>(instance);
                return instance;
            }
            return (OutputStreamWrappers)descriptor.ImplementationInstance;

        }

        private static ILoggingBuilder AddFormattedString<TLoggerProvider>(this ILoggingBuilder builder, string outputStreamWrapperName)
            where TLoggerProvider : LoggerProvider
        {
            builder.Services.AddSingleton<ILoggerProvider>(serviceProvider =>
            {
                var outputStreamWrapper = serviceProvider.GetRequiredService<IOutputStreamWrappers>()
                    .Create(serviceProvider, outputStreamWrapperName);
                var payloadFactory = new FormattedStringPayloadFactory();
                var payloadWriter = new FormattedStringPayloadWriter(outputStreamWrapper);
                var sink = new FormattedStringSink(payloadWriter, payloadFactory);
                return ActivatorUtilities.CreateInstance<TLoggerProvider>(serviceProvider, sink);
            });
            return builder;
        }

        public static ILoggingBuilder AddFormattedRollingFile<TLoggerProvider>(this ILoggingBuilder builder, string path)
            where TLoggerProvider : LoggerProvider
        {
            var name = $"wrp_{Interlocked.Increment(ref _idSupply)}";
            builder.Services.GetOrAddOutputStreamWrappers()
                .Add(name, _ => new RollingFileStreamWrapper(path));
            return builder.AddFormattedString<TLoggerProvider>(name);
        }
    }
}
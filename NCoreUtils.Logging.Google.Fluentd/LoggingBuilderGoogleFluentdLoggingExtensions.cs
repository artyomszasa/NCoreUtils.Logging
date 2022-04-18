using System;
using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NCoreUtils.Logging.Google;
using NCoreUtils.Logging.Google.Internal;

namespace NCoreUtils.Logging
{
    public static class LoggingBuilderGoogleFluentdLoggingExtensions
    {
        private sealed class LoggerProviderInitializer<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] TLoggerProvider>
            where TLoggerProvider : LoggerProvider
        {
            public string OptionsName { get; }

            public LoggerProviderInitializer(string optionsName)
                => OptionsName = optionsName;

            public ILoggerProvider Initialize(IServiceProvider serviceProvider)
            {
                var options = serviceProvider.GetRequiredService<IOptionsMonitor<GoogleFluentdSinkOptions>>().Get(OptionsName);
                var output = serviceProvider.CreateByteSequenceOutput(options.Configuration.Output);
                var payloadWriter = options.CreatePayloadWriter(serviceProvider, output);
                var payloadFactory = options.CreatePayloadFactory(serviceProvider);
                var sink = options.CreateSink(serviceProvider, payloadWriter, payloadFactory);
                return ActivatorUtilities.CreateInstance<TLoggerProvider>(serviceProvider, sink);
            }
        }

        public static ILoggingBuilder AddGoogleFluentd<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] TLoggerProvider>(
            this ILoggingBuilder builder,
            string? name,
            IGoogleFluentdSinkConfiguration configuration,
            Action<GoogleFluentdSinkOptions>? configureOptions = default)
            where TLoggerProvider : LoggerProvider
        {
            // options
            var optionsName = name ?? Options.DefaultName;
            var opts = builder.Services
                .AddOptions<GoogleFluentdSinkOptions>(optionsName)
                .Configure(o => o.Configuration = configuration);
            if (configureOptions is not null)
            {
                opts.Configure(o => configureOptions(o));
            }
            builder.Services.AddSingleton(new LoggerProviderInitializer<TLoggerProvider>(optionsName).Initialize);
            return builder;
        }

        public static ILoggingBuilder AddGoogleFluentd<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] TLoggerProvider>(
            this ILoggingBuilder builder,
            IGoogleFluentdSinkConfiguration configuration,
            Action<GoogleFluentdSinkOptions>? configureOptions = default)
            where TLoggerProvider : LoggerProvider
            => builder.AddGoogleFluentd<TLoggerProvider>(default, configuration, configureOptions);

        public static ILoggingBuilder AddGoogleFluentd(
            this ILoggingBuilder builder,
            IGoogleFluentdSinkConfiguration configuration,
            Action<GoogleFluentdSinkOptions>? configureOptions = default)
            => builder.AddGoogleFluentd<LoggerProvider>(configuration, configureOptions);

        public static ILoggingBuilder AddGoogleFluentd<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] TLoggerProvider>(
            this ILoggingBuilder builder,
            string? name,
            string output = DefaultByteSequenceOutput.StdOut,
            string? projectId = default,
            string? service = default,
            string? serviceVersion = default,
            CategoryHandling? categoryHandling = default,
            EventIdHandling? eventIdHandling = default,
            TraceHandling? traceHandling = default,
            Action<GoogleFluentdSinkOptions>? configureOptions = default)
            where TLoggerProvider : LoggerProvider
        {
            var context = GoogleLoggingInitialization.InitializeGoogleLoggingContext(
                projectId,
                service,
                serviceVersion
            );
            var options = new GoogleFluentdSinkConfiguration
            {
                Output = output,
                CategoryHandling = categoryHandling ?? CategoryHandling.IncludeAsLabel,
                EventIdHandling = eventIdHandling ?? EventIdHandling.Ignore,
                ProjectId = context.ProjectId,
                Service = context.Service,
                ServiceVersion = context.ServiceVersion,
                TraceHandling = traceHandling ?? TraceHandling.Summary
            };
            return builder.AddGoogleFluentd<TLoggerProvider>(name, options, configureOptions);
        }

        public static ILoggingBuilder AddGoogleFluentd<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] TLoggerProvider>(
            this ILoggingBuilder builder,
            string output = DefaultByteSequenceOutput.StdOut,
            string? projectId = default,
            string? service = default,
            string? serviceVersion = default,
            CategoryHandling? categoryHandling = default,
            EventIdHandling? eventIdHandling = default,
            TraceHandling? traceHandling = default,
            Action<GoogleFluentdSinkOptions>? configureOptions = default)
            where TLoggerProvider : LoggerProvider
            => builder.AddGoogleFluentd<TLoggerProvider>(
                default,
                output,
                projectId,
                service,
                serviceVersion,
                categoryHandling,
                eventIdHandling,
                traceHandling,
                configureOptions
            );

        public static ILoggingBuilder AddGoogleFluentd(
            this ILoggingBuilder builder,
            string output = DefaultByteSequenceOutput.StdOut,
            string? projectId = default,
            string? service = default,
            string? serviceVersion = default,
            CategoryHandling? categoryHandling = default,
            EventIdHandling? eventIdHandling = default,
            TraceHandling? traceHandling = default,
            Action<GoogleFluentdSinkOptions>? configureOptions = default)
            => builder.AddGoogleFluentd<LoggerProvider>(
                output,
                projectId,
                service,
                serviceVersion,
                categoryHandling,
                eventIdHandling,
                traceHandling,
                configureOptions
            );

#if !NETSTANDARD2_1
        [UnconditionalSuppressMessage("ReflectionAnalysis", "IL2026",
            Justification = "Enumerations are bound to the configuration type.")]
#endif
        public static ILoggingBuilder AddGoogleFluentd<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] TLoggerProvider>(
            this ILoggingBuilder builder,
            string? name,
            IConfiguration configuration,
            Action<GoogleFluentdSinkOptions>? configureOptions = default)
            where TLoggerProvider : LoggerProvider
        {
            var context = GoogleLoggingInitialization.InitializeGoogleLoggingContext(
                configuration["ProjectId"],
                configuration["Service"],
                configuration["ServiceVersion"]
            );
            return builder.AddGoogleFluentd<TLoggerProvider>(name, new GoogleFluentdSinkConfiguration
            {
                Output = configuration["Output"] ?? DefaultByteSequenceOutput.StdOut,
                CategoryHandling = configuration.GetValue<CategoryHandling?>("CategoryHandling") ?? CategoryHandling.IncludeAsLabel,
                EventIdHandling = configuration.GetValue<EventIdHandling?>("EventIdHandling") ?? EventIdHandling.Ignore,
                ProjectId = context.ProjectId,
                Service = context.Service,
                ServiceVersion = context.ServiceVersion,
                TraceHandling = configuration.GetValue<TraceHandling?>("TraceHandling") ?? TraceHandling.Summary
            }, configureOptions);
        }

        public static ILoggingBuilder AddGoogleFluentd<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] TLoggerProvider>(
            this ILoggingBuilder builder,
            IConfiguration configuration,
            Action<GoogleFluentdSinkOptions>? configureOptions = default)
            where TLoggerProvider : LoggerProvider
            => builder.AddGoogleFluentd<TLoggerProvider>(
                default,
                configuration,
                configureOptions
            );

        public static ILoggingBuilder AddGoogleFluentd(
            this ILoggingBuilder builder,
            IConfiguration configuration,
            Action<GoogleFluentdSinkOptions>? configureOptions = default)
            => builder.AddGoogleFluentd<LoggerProvider>(configuration, configureOptions);
    }
}
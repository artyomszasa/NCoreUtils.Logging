using System.Collections.Generic;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NCoreUtils.Logging.Google;
using NCoreUtils.Logging.Google.Internal;

namespace NCoreUtils.Logging
{
    public static class LoggingBuilderGoogleClientLoggingExtensions
    {
        public static ILoggingBuilder AddGoogleClient<TLoggerProvider>(
            this ILoggingBuilder builder,
            IGoogleClientSinkConfiguration configuration)
            where TLoggerProvider : LoggerProvider
        {
            builder.Services.AddSingleton<ILoggerProvider>(serviceProvider =>
            {
                var payloadWriter = ActivatorUtilities.CreateInstance<GoogleClientPayloadWriter>(serviceProvider, configuration);
                var payloadFactory = ActivatorUtilities.CreateInstance<GoogleClientPayloadFactory>(serviceProvider, configuration);
                var sink = new GoogleClientSink(payloadWriter, payloadFactory);
                return ActivatorUtilities.CreateInstance<TLoggerProvider>(serviceProvider, sink);
            });
            return builder;
        }

        public static ILoggingBuilder AddGoogleClient(this ILoggingBuilder builder, IGoogleClientSinkConfiguration options)
            => builder.AddGoogleClient<LoggerProvider>(options);

        public static ILoggingBuilder AddGoogleClient<TLoggerProvider>(
            this ILoggingBuilder builder,
            string? projectId = default,
            string? service = default,
            string? serviceVersion = default,
            string? resourceType = default,
            IReadOnlyDictionary<string, string>? resourceLabels = default,
            CategoryHandling? categoryHandling = default,
            EventIdHandling? eventIdHandling = default,
            TraceHandling? traceHandling = default)
            where TLoggerProvider : LoggerProvider
        {
            var context = GoogleClientLoggingInitialization.InitializeGoogleLoggingContext(
                projectId,
                service,
                serviceVersion,
                resourceType,
                resourceLabels
            );
            return builder.AddGoogleClient<TLoggerProvider>(new GoogleClientSinkConfiguration
            {
                CategoryHandling = categoryHandling ?? CategoryHandling.IncludeAsLabel,
                EventIdHandling = eventIdHandling ?? EventIdHandling.Ignore,
                ProjectId = context.ProjectId,
                Resource = context.Resource,
                Service = context.Service,
                ServiceVersion = context.ServiceVersion,
                TraceHandling = traceHandling ?? TraceHandling.Summary
            });
        }

        public static ILoggingBuilder AddGoogleClient(
            this ILoggingBuilder builder,
            string? projectId = default,
            string? service = default,
            string? serviceVersion = default,
            string? resourceType = default,
            IReadOnlyDictionary<string, string>? resourceLabels = default,
            CategoryHandling? categoryHandling = default,
            EventIdHandling? eventIdHandling = default,
            TraceHandling? traceHandling = default)
            => builder.AddGoogleClient<LoggerProvider>(
                projectId,
                service,
                serviceVersion,
                resourceType,
                resourceLabels,
                categoryHandling,
                eventIdHandling,
                traceHandling
            );

        public static ILoggingBuilder AddGoogleClient<TLoggerProvider>(
            this ILoggingBuilder builder,
            IConfiguration configuration)
            where TLoggerProvider : LoggerProvider
        {
            var context = GoogleClientLoggingInitialization.InitializeGoogleLoggingContext(
                configuration["ProjectId"],
                configuration["Service"],
                configuration["ServiceVersion"],
                configuration["ResourceType"],
                configuration.GetSection("ResourceLabels").Get<Dictionary<string, string>>()
            );
            return builder.AddGoogleClient<TLoggerProvider>(new GoogleClientSinkConfiguration
            {
                CategoryHandling = configuration.GetValue<CategoryHandling?>("CategoryHandling") ?? CategoryHandling.IncludeAsLabel,
                EventIdHandling = configuration.GetValue<EventIdHandling?>("EventIdHandling") ?? EventIdHandling.Ignore,
                ProjectId = context.ProjectId,
                Resource = context.Resource,
                Service = context.Service,
                ServiceVersion = context.ServiceVersion,
                TraceHandling = configuration.GetValue<TraceHandling?>("TraceHandling") ?? TraceHandling.Summary
            });
        }

        public static ILoggingBuilder AddGoogleClient(this ILoggingBuilder builder, IConfiguration configuration)
            => builder.AddGoogleClient<LoggerProvider>(configuration);
    }
}
using System.Diagnostics.CodeAnalysis;
using Google.Api;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NCoreUtils.Logging.Google;
using NCoreUtils.Logging.Google.Internal;

namespace NCoreUtils.Logging;

public static class LoggingBuilderGoogleClientLoggingExtensions
{
    private static Dictionary<string, string> GetDictionaryOfStringAndString(this IConfiguration configuration)
    {
        var result = new Dictionary<string, string>();
        foreach (var (key, value) in configuration.AsEnumerable(makePathsRelative: true))
        {
            result.Add(key, value ?? string.Empty);
        }
        return result;
    }

    public static ILoggingBuilder AddGoogleClient<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] TLoggerProvider>(
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

    public static ILoggingBuilder AddGoogleClient<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] TLoggerProvider>(
        this ILoggingBuilder builder,
        string? projectId = default,
        string? service = default,
        string? serviceVersion = default,
        CategoryHandling? categoryHandling = default,
        EventIdHandling? eventIdHandling = default,
        TraceHandling? traceHandling = default,
        IGoogleLoggingInitializer? initializer = default)
        where TLoggerProvider : LoggerProvider
    {
        var context = (initializer ?? new GoogleClientLoggingInitializer()).InitializeGoogleLoggingContext(
            inputProjectId: projectId,
            inputService: service,
            inputServiceVersion: serviceVersion,
            preferConfig: true
        );
        if (context.Details is not MonitoredResource monitoredResource)
        {
            throw new InvalidOperationException("Google logging initializer did not return MonitoredResource as Details. Consider using custom initializer when relevant.");
        }
        return builder.AddGoogleClient<TLoggerProvider>(new GoogleClientSinkConfiguration
        {
            CategoryHandling = categoryHandling ?? CategoryHandling.IncludeAsLabel,
            EventIdHandling = eventIdHandling ?? EventIdHandling.Ignore,
            ProjectId = context.ProjectId,
            Resource = monitoredResource,
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
        CategoryHandling? categoryHandling = default,
        EventIdHandling? eventIdHandling = default,
        TraceHandling? traceHandling = default,
        IGoogleLoggingInitializer? initializer = default)
        => builder.AddGoogleClient<LoggerProvider>(
            projectId,
            service,
            serviceVersion,
            categoryHandling,
            eventIdHandling,
            traceHandling,
            initializer
        );

    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(Dictionary<string, string>))]
    [UnconditionalSuppressMessage("ReflectionAnalysis", "IL2026",
        Justification = "Dictionary is preserved explicitly, enumerations are bound to the configuration type.")]
    public static ILoggingBuilder AddGoogleClient<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] TLoggerProvider>(
        this ILoggingBuilder builder,
        IConfiguration configuration,
        IGoogleLoggingInitializer? initializer = default)
        where TLoggerProvider : LoggerProvider
    {
        var context = (initializer ?? new GoogleClientLoggingInitializer()).InitializeGoogleLoggingContext(
            inputProjectId: configuration["ProjectId"],
            inputService: configuration["Service"],
            inputServiceVersion: configuration["ServiceVersion"],
            preferConfig: true
        );
        if (context.Details is not MonitoredResource monitoredResource)
        {
            throw new InvalidOperationException("Google logging initializer did not return MonitoredResource as Details. Consider using custom initializer when relevant.");
        }
        return builder.AddGoogleClient<TLoggerProvider>(new GoogleClientSinkConfiguration
        {
            CategoryHandling = configuration.GetCategoryHandlingOrNull("CategoryHandling") ?? CategoryHandling.IncludeAsLabel,
            EventIdHandling = configuration.GetEventIdHandlingOrNull("EventIdHandling") ?? EventIdHandling.Ignore,
            ProjectId = context.ProjectId,
            Resource = monitoredResource,
            Service = context.Service,
            ServiceVersion = context.ServiceVersion,
            TraceHandling = configuration.GetTraceHandlingOrNull("TraceHandling") ?? TraceHandling.Summary
        });
    }

    public static ILoggingBuilder AddGoogleClient(
        this ILoggingBuilder builder,
        IConfiguration configuration,
        IGoogleLoggingInitializer? initializer = default)
        => builder.AddGoogleClient<LoggerProvider>(configuration, initializer);
}
using System;
using System.Collections.Generic;
using System.Reflection;
using Google.Api;
using Google.Api.Gax;
using Google.Api.Gax.Grpc;
using Google.Cloud.Logging.V2;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NCoreUtils.Logging;
using NCoreUtils.Logging.Google;

namespace NCoreUtils.AspNetCore
{
    public static class LoggerBuilderAspNetCoreGoogleSinkLoggingExtensions
    {
        private sealed class AspNetCoreConnectionIdLabelProvider : ILabelProvider
        {
            public void UpdateLabels(string category, EventId eventId, LogLevel logLevel, in AspNetCoreContext context, IDictionary<string, string> labels)
            {
                if (!string.IsNullOrEmpty(context.ConnectionId))
                {
                    labels.Add("aspnetcore-connection-id", context.ConnectionId);
                }
            }
        }

        private static string? FirstNonEmpty(string? a, string? b)
        {
            if (!string.IsNullOrEmpty(a))
            {
                return a;
            }
            if (!string.IsNullOrEmpty(b))
            {
                return b;
            }
            return default;
        }

        public static ILoggingBuilder AddGoogleSink(this ILoggingBuilder builder, AspNetCoreGoogleLoggingContext context)
        {
            builder.Services.AddLoggingContext();
            builder.Services.AddSingleton(context);
            return builder
                .AddGoogleLabelProvider(new AspNetCoreConnectionIdLabelProvider())
                .AddSink<AspNetCoreLoggerProvider<AspNetCoreGoogleSink>, AspNetCoreGoogleSink>();
        }

        public static ILoggingBuilder AddGoogleSink(
            this ILoggingBuilder builder,
            IGoogleAspNetCoreLoggingConfiguration? configuration = default,
            bool force = false)
        {
            var platform = Platform.Instance();
            Console.WriteLine($"Configuring google logging: platform = {platform}, config = {configuration}, force = {force}.");
            LogName logName;
            MonitoredResource resource;
            string? serviceVersion;
            if (force)
            {
                var projectId = FirstNonEmpty(configuration?.ProjectId, platform.ProjectId) ?? throw new InvalidOperationException("No project id found.");
                var logId = FirstNonEmpty(configuration?.ServiceName, platform.GetServiceName()) ?? Assembly.GetEntryAssembly()?.GetName().Name?.Replace(".", "-")?.ToLowerInvariant();
                logName = new LogName(projectId, logId);
                resource = configuration?.ResourceType switch
                {
                    null => MonitoredResourceBuilder.FromPlatform(platform),
                    string resourceType => new MonitoredResource()
                        .WithType(resourceType)
                        .WithLabels(configuration?.ResourceLabels)
                };
                serviceVersion = configuration?.ServiceVersion ?? platform.GetServiceVersion();
            }
            else
            {
                var projectId = FirstNonEmpty(platform.ProjectId, configuration?.ProjectId) ?? throw new InvalidOperationException("No project id found.");
                var logId = FirstNonEmpty(platform.GetServiceName(), configuration?.ServiceName) ?? Assembly.GetEntryAssembly()?.GetName()?.Name?.Replace(".", "-")?.ToLowerInvariant();
                logName = new LogName(projectId, logId);
                if (platform.Type != PlatformType.Unknown || configuration?.ResourceType is null)
                {
                    resource = MonitoredResourceBuilder.FromPlatform(platform);
                }
                else
                {
                    resource = new MonitoredResource()
                        .WithType(configuration!.ResourceType!)
                        .WithLabels(configuration?.ResourceLabels);
                }
                serviceVersion = FirstNonEmpty(platform.GetServiceVersion(), configuration?.ServiceVersion);
            }
            return builder.AddGoogleSink(new AspNetCoreGoogleLoggingContext(
                logName,
                resource,
                serviceVersion,
                configuration?.CategoryHandling ?? CategoryHandling.IncludeAsLabel,
                configuration?.EventIdHandling ?? EventIdHandling.IncludeValidIds
            ));
        }

        public static ILoggingBuilder AddGoogleSink(
            this ILoggingBuilder builder,
            IConfiguration configuration,
            bool force = false)
        {
            var loggingConfiguration = new GoogleAspNetCoreLoggingConfiguration();
            configuration.Bind(loggingConfiguration);
            return builder.AddGoogleSink(loggingConfiguration, force);
        }

        public static ILoggingBuilder AddGoogleSink(
            this ILoggingBuilder builder,
            string projectId,
            string? serviceName = default,
            string? serviceVersion = default,
            string? resourceType = default,
            IReadOnlyDictionary<string, string>? labels = default,
            bool force = false,
            CategoryHandling categoryHandling = CategoryHandling.IncludeAsLabel,
            EventIdHandling eventIdHandling = EventIdHandling.IncludeValidIds)
        {
            var loggingConfiguration = new GoogleAspNetCoreLoggingConfiguration
            {
                ProjectId = projectId,
                ServiceName = serviceName,
                ServiceVersion = serviceVersion,
                ResourceType = resourceType,
                CategoryHandling = categoryHandling,
                EventIdHandling = eventIdHandling
            };
            if (null != labels)
            {
                foreach (var kv in labels)
                {
                    loggingConfiguration.ResourceLabels.Add(kv.Key, kv.Value);
                }
            }
            return builder.AddGoogleSink(loggingConfiguration, force);
        }

        public static ILoggingBuilder AddGoogleLabelProvider(this ILoggingBuilder builder, ILabelProvider provider)
        {
            builder.Services.AddSingleton<ILabelProvider>(provider);
            return builder;
        }

        public static ILoggingBuilder AddGoogleLabelProvider(this ILoggingBuilder builder, Action<string, EventId, LogLevel, AspNetCoreContext, IDictionary<string, string>> provider)
            => builder.AddGoogleLabelProvider(LabelProvider.Create(provider));

        public static ILoggingBuilder AddGoogleLabelProvider(this ILoggingBuilder builder, Action<AspNetCoreContext, IDictionary<string, string>> provider)
            => builder.AddGoogleLabelProvider(LabelProvider.Create(provider));
    }
}
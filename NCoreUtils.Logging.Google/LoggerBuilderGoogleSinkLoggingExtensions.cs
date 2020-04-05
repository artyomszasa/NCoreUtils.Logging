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
using NCoreUtils.Logging.Google;

namespace NCoreUtils.Logging
{
    public static class LoggerBuilderGoogleSinkLoggingExtensions
    {


        public static ILoggingBuilder AddGoogleSink(this ILoggingBuilder builder, GoogleLoggingContext context)
        {
            builder.Services.AddSingleton(context);
            return builder.AddSink<GoogleSink>();
        }

        public static ILoggingBuilder AddGoogleSink(
            this ILoggingBuilder builder,
            IGoogleLoggingConfiguration? configuration = default,
            bool force = false)
        {
            var platform = Platform.Instance();
            LogName logName;
            MonitoredResource resource;
            if (force)
            {
                var projectId = configuration?.ProjectId ?? platform.ProjectId ?? throw new InvalidOperationException("No project id found.");
                var logId = configuration?.LogId ?? platform.GetServiceName() ?? Assembly.GetEntryAssembly().GetName().Name.Replace(".", "-").ToLowerInvariant();
                logName = new LogName(projectId, logId);
                resource = configuration?.ResourceType switch
                {
                    null => MonitoredResourceBuilder.FromPlatform(platform),
                    string resourceType => new MonitoredResource()
                        .WithType(resourceType)
                        .WithLabels(configuration?.ResourceLabels)
                };
            }
            else
            {
                var projectId = platform.ProjectId ?? configuration?.ProjectId ?? throw new InvalidOperationException("No project id found.");
                var logId = platform.GetServiceName() ?? configuration?.LogId ?? Assembly.GetEntryAssembly().GetName().Name.Replace(".", "-").ToLowerInvariant();
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
            }
            return builder.AddGoogleSink(new GoogleLoggingContext(logName, resource));
        }

        public static ILoggingBuilder AddGoogleSink(
            this ILoggingBuilder builder,
            IConfiguration configuration,
            bool force = false)
        {
            var loggingConfiguration = new GoogleLoggingConfiguration();
            configuration.Bind(loggingConfiguration);
            return builder.AddGoogleSink(loggingConfiguration, force);
        }

        public static ILoggingBuilder AddGoogleSink(
            this ILoggingBuilder builder,
            string projectId,
            string? logId = default,
            string? resourceType = default,
            IReadOnlyDictionary<string, string>? labels = default,
            bool force = false)
        {
            var loggingConfiguration = new GoogleLoggingConfiguration
            {
                ProjectId = projectId,
                LogId = logId,
                ResourceType = resourceType
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
    }
}
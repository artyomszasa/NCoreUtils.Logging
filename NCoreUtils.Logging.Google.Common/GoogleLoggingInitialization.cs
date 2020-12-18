using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using Google.Api;
using Google.Api.Gax;

namespace NCoreUtils.Logging.Google.Internal
{
    public static class GoogleLoggingInitialization
    {
        [return: NotNullIfNotNull("b")]
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

        public static MonitoredResource WithType(this MonitoredResource resource, string type)
        {
            resource.Type = type;
            return resource;
        }

        public static MonitoredResource WithLabels(this MonitoredResource resource, IReadOnlyDictionary<string, string>? labels)
        {
            if (labels != null)
            {
                foreach (var kv in labels)
                {
                    resource.Labels.Add(kv.Key, kv.Value);
                }
            }
            return resource;
        }

        public static string? GetServiceName(this Platform platform) => platform.Type switch
        {
            PlatformType.CloudRun => platform.CloudRunDetails.ServiceName,
            PlatformType.Gae => platform.GaeDetails.ServiceId,
            PlatformType.Gce => platform.GceDetails.InstanceId,
            PlatformType.Gke => platform.GkeDetails.ContainerName,
            _ => default
        };

        public static string? GetServiceVersion(this Platform platform) => platform.Type switch
        {
            PlatformType.CloudRun => platform.CloudRunDetails.RevisionName,
            PlatformType.Gae => platform.GaeDetails.VersionId,
            _ => default
        };

        public static GoogleLoggingContext InitializeGoogleLoggingContext(
            Lazy<Platform> p,
            string? inputProjectId,
            string? inputService,
            string? inputServiceVersion,
            // string? inputResourceType,
            // IReadOnlyDictionary<string, string>? inputResourceLabels,
            bool preferConfig = false)
        {
            string projectId;
            string service;
            string? serviceVersion;
            // MonitoredResource resource;
            if (preferConfig)
            {
                projectId = FirstNonEmpty(inputProjectId, p.Value.ProjectId)
                    ?? throw new InvalidOperationException("Unable to get GCP project ID. Consider providing explicit value.");
                service = FirstNonEmpty(inputService, p.Value.GetServiceName())
                    ?? Assembly.GetEntryAssembly()?.GetName().Name?.Replace(".", "-")?.ToLowerInvariant()
                    ?? throw new InvalidOperationException("Unable to get service name. Consider providing explicit value.");
                serviceVersion = inputServiceVersion ?? p.Value.GetServiceVersion();
            }
            else
            {
                projectId = FirstNonEmpty(p.Value.ProjectId, inputProjectId)
                    ?? throw new InvalidOperationException("Unable to get GCP project ID. Consider providing explicit value.");
                service = FirstNonEmpty(p.Value.GetServiceName(), inputService)
                    ?? Assembly.GetEntryAssembly()?.GetName()?.Name?.Replace(".", "-")?.ToLowerInvariant()
                    ?? throw new InvalidOperationException("Unable to get service name. Consider providing explicit value.");
                serviceVersion = FirstNonEmpty(p.Value.GetServiceVersion(), inputServiceVersion);
            }
            return new GoogleLoggingContext(projectId, service, serviceVersion);
        }

        public static GoogleLoggingContext InitializeGoogleLoggingContext(
            string? inputProjectId,
            string? inputService,
            string? inputServiceVersion,
            // string? inputResourceType,
            // IReadOnlyDictionary<string, string>? inputResourceLabels,
            bool preferConfig = false)
        {
            var p = new Lazy<Platform>(() => Platform.Instance(), false);
            return InitializeGoogleLoggingContext(p, inputProjectId, inputService, inputServiceVersion, preferConfig);
        }
    }
}
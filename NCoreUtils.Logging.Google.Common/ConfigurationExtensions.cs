using System.Collections.Generic;
using Google.Api;
using Google.Api.Gax;

namespace NCoreUtils.Logging
{
    public static class ConfigurationExtensions
    {
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
    }
}
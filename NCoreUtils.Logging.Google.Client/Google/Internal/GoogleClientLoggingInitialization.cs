using System;
using System.Collections.Generic;
using Google.Api;
using Google.Api.Gax;
using Google.Api.Gax.Grpc;

namespace NCoreUtils.Logging.Google.Internal
{
    public static class GoogleClientLoggingInitialization
    {
        public static GoogleClientLoggingContext InitializeGoogleLoggingContext(
            string? inputProjectId,
            string? inputService,
            string? inputServiceVersion,
            string? inputResourceType,
            IReadOnlyDictionary<string, string>? inputResourceLabels,
            bool preferConfig = false)
        {
            var p = new Lazy<Platform>(() => Platform.Instance(), false);
            var context = GoogleLoggingInitialization.InitializeGoogleLoggingContext(p, inputProjectId, inputService, inputServiceVersion, preferConfig);
            MonitoredResource resource;
            if (preferConfig)
            {
                resource = inputResourceType switch
                {
                    null => MonitoredResourceBuilder.FromPlatform(p.Value),
                    string resourceType => new MonitoredResource()
                        .WithType(resourceType)
                        .WithLabels(inputResourceLabels)
                };
            }
            else
            {
                if (p.Value.Type != PlatformType.Unknown || inputResourceType is null)
                {
                    resource = MonitoredResourceBuilder.FromPlatform(p.Value);
                }
                else
                {
                    resource = new MonitoredResource()
                        .WithType(inputResourceType!)
                        .WithLabels(inputResourceLabels);
                }
            }
            return new GoogleClientLoggingContext(context, resource);
        }
    }
}
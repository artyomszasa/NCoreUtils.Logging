using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using Google.Api.Gax;
using Google.Api.Gax.Grpc;

namespace NCoreUtils.Logging.Google.Internal;

public class GoogleClientLoggingInitializer : IGoogleLoggingInitializer
{
    [return: NotNullIfNotNull(nameof(a))]
    [return: NotNullIfNotNull(nameof(b))]
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

    [return: NotNullIfNotNull(nameof(a))]
    [return: NotNullIfNotNull(nameof(b))]
    [return: NotNullIfNotNull(nameof(c))]
    private static string? FirstNonEmpty(string? a, string? b, string? c)
    {
        if (!string.IsNullOrEmpty(a)) { return a; }
        if (!string.IsNullOrEmpty(b)) { return b; }
        if (!string.IsNullOrEmpty(c)) { return c; }
        return default;
    }

    public GoogleLoggingContext InitializeGoogleLoggingContext(
        string? inputProjectId,
        string? inputService,
        string? inputServiceVersion,
        bool preferConfig = false)
    {
        var p = Platform.Instance();
        string projectId;
        string service;
        string? serviceVersion;
        // MonitoredResource resource;
        if (preferConfig)
        {
            projectId = FirstNonEmpty(inputProjectId, p.ProjectId)
                ?? throw new InvalidOperationException("Unable to get GCP project ID. Consider providing explicit value.");
            service = FirstNonEmpty(inputService, p.GaeDetails?.ServiceId, p.GkeDetails?.ContainerName)
                ?? Assembly.GetEntryAssembly()?.GetName().Name?.Replace(".", "-")?.ToLowerInvariant()
                ?? throw new InvalidOperationException("Unable to get service name. Consider providing explicit value.");
            serviceVersion = FirstNonEmpty(inputServiceVersion, p.GaeDetails?.VersionId);
        }
        else
        {
            projectId = FirstNonEmpty(p.ProjectId, inputProjectId)
                ?? throw new InvalidOperationException("Unable to get GCP project ID. Consider providing explicit value.");
            service = FirstNonEmpty(p.GaeDetails?.ServiceId, p.GkeDetails?.ContainerName, inputService)
                ?? Assembly.GetEntryAssembly()?.GetName()?.Name?.Replace(".", "-")?.ToLowerInvariant()
                ?? throw new InvalidOperationException("Unable to get service name. Consider providing explicit value.");
            serviceVersion = FirstNonEmpty(p.GaeDetails?.VersionId, inputServiceVersion);
        }
        return new GoogleLoggingContext(projectId, service, serviceVersion, MonitoredResourceBuilder.FromPlatform(p));
    }
}

/*
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
        var context = GoogleLoggingInitialization.InitializeGoogleLoggingContext(
            // FIXME: this should work as a workaround due to definition equality, yet better solution is required
            Unsafe.As<Lazy<global::Google.Api.Gax.STJ.Platform>>(p),
            inputProjectId,
            inputService,
            inputServiceVersion,
            preferConfig);
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
*/
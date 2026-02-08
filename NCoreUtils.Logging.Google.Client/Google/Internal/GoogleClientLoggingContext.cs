using Google.Api;

namespace NCoreUtils.Logging.Google.Internal;

[Obsolete]
public readonly struct GoogleClientLoggingContext(string projectId, string? service, string? serviceVersion, MonitoredResource resource)
{
    public string ProjectId { get; } = projectId;

    public string? Service { get; } = service;

    public string? ServiceVersion { get; } = serviceVersion;

    public MonitoredResource Resource { get; } = resource;

    public GoogleClientLoggingContext(in GoogleLoggingContext context, MonitoredResource resource)
        : this (context.ProjectId, context.Service, context.ServiceVersion, resource)
    { }
}
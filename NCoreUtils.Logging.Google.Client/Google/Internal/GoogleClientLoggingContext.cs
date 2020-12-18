using Google.Api;

namespace NCoreUtils.Logging.Google.Internal
{
    public struct GoogleClientLoggingContext
    {
        public string ProjectId { get; }

        public string Service { get; }

        public string? ServiceVersion { get; }

        public MonitoredResource Resource { get; }

        public GoogleClientLoggingContext(string projectId, string service, string? serviceVersion, MonitoredResource resource)
        {
            ProjectId = projectId;
            Service = service;
            ServiceVersion = serviceVersion;
            Resource = resource;
        }

        public GoogleClientLoggingContext(in GoogleLoggingContext context, MonitoredResource resource)
            : this (context.ProjectId, context.Service, context.ServiceVersion, resource)
        { }
    }
}
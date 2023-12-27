namespace NCoreUtils.Logging.Google.Internal;

public readonly struct GoogleLoggingContext
{
    public string ProjectId { get; }

    public string Service { get; }

    public string? ServiceVersion { get; }

    public GoogleLoggingContext(string projectId, string service, string? serviceVersion)
    {
        ProjectId = projectId;
        Service = service;
        ServiceVersion = serviceVersion;
    }
}
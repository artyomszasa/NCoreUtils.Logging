namespace NCoreUtils.Logging.Google.Internal;

public readonly struct GoogleLoggingContext(
    string projectId,
    string? service,
    string? serviceVersion,
    object? details = default)
{
    public string ProjectId { get; } = projectId;

    public string? Service { get; } = service;

    public string? ServiceVersion { get; } = serviceVersion;

    public object? Details { get; } = details;
}
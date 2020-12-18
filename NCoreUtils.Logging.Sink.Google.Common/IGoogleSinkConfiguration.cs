namespace NCoreUtils.Logging.Google
{
    public interface IGoogleSinkConfiguration
    {
        /// <summary>
        /// GCP project identifier.
        /// </summary>
        string ProjectId { get; }

        /// <summary>
        /// Service identifier.
        /// </summary>
        string Service { get; }

        /// <summary>
        /// Optional service version.
        /// </summary>
        string? ServiceVersion { get; }

        /// <summary>
        /// Computed log name.
        /// </summary>
        string LogName { get; }

        /// <summary>
        /// How categories should be handled while generating a log entry.
        /// </summary>
        CategoryHandling CategoryHandling { get; }

        /// <summary>
        /// How event ids should be handled while generating a log entry.
        /// </summary>
        EventIdHandling EventIdHandling { get; }

        /// <summary>
        /// Whether to include trace information in log entries. Defaults to <c>Summary</c>.
        /// </summary>
        TraceHandling TraceHandling { get; }
    }
}
namespace NCoreUtils.AspNetCore
{
    public enum TraceHandling
    {
        /// Do not include any tracing information in log entries.
        Disabled = 0,
        // Only include tracing information in request summary entries.
        Summary = 1,
        // Always include tracing information in log entries.
        Enabled = 2
    }
}
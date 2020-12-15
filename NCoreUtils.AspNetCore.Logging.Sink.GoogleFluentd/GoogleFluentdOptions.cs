using System.Text.Json;
using NCoreUtils.AspNetCore;

namespace NCoreUtils.Logging.Google
{
    /// <summary>
    /// Google Fluentd related options.
    /// </summary>
    public class GoogleFluentdOptions
    {
        /// <summary>
        /// Whether to include trace information in log entries. Defaults to <c>Summary</c>.
        /// </summary>
        public TraceHandling TraceHandling { get; set; } = TraceHandling.Summary;

        /// <summary>
        /// How categories should be handled while generating a log entry.
        /// </summary>
        public CategoryHandling CategoryHandling { get; set; } = CategoryHandling.IncludeAsLabel;

        /// <summary>
        /// How event ids should be handled while generating a log entry.
        /// </summary>
        public EventIdHandling EventIdHandling { get; set; } = EventIdHandling.IncludeValidIds;

        /// <summary>
        /// Serialization options used to serialize log entry as json.
        /// </summary>
        public JsonSerializerOptions JsonSerializerOptions { get; } = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };
    }
}
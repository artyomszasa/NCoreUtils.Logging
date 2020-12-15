using System;

namespace NCoreUtils.Logging.Google
{
    public class AspNetCoreGoogleFluentdLoggingContext
    {
        /// <summary>
        /// Fluentd configuration as Uri. e.g. <c>file:///dev/stdout</c> or <c>tcp://0.0.0.0:5170</c>
        /// </summary>
        public string FluentdUri { get; }

        public string ProjectId { get; }

        public string LogId { get; }

        public string? ServiceVersion { get; }

        public AspNetCoreGoogleFluentdLoggingContext(
            string fluentdUri,
            string projectId,
            string logId,
            string? serviceVersion)
        {
            FluentdUri = fluentdUri;
            ProjectId = projectId ?? throw new ArgumentNullException(nameof(projectId));
            LogId = logId ?? throw new ArgumentNullException(nameof(logId));
            ServiceVersion = serviceVersion;
        }
    }
}
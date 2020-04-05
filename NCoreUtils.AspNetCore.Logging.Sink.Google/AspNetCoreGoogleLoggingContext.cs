using System;
using Google.Api;
using Google.Cloud.Logging.V2;

namespace NCoreUtils.Logging.Google
{
    public class AspNetCoreGoogleLoggingContext
    {
        public LogName LogName { get; }

        public MonitoredResource Resource { get; }

        public string? ServiceVersion { get; }

        public AspNetCoreGoogleLoggingContext(LogName logName, MonitoredResource resource, string? serviceVersion)
        {
            LogName = logName ?? throw new ArgumentNullException(nameof(logName));
            Resource = resource ?? throw new ArgumentNullException(nameof(resource));
            ServiceVersion = serviceVersion;
        }
    }
}
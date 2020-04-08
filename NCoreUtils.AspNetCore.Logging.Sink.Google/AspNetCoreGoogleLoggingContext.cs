using System;
using Google.Api;
using Google.Cloud.Logging.V2;
using NCoreUtils.AspNetCore;

namespace NCoreUtils.Logging.Google
{
    public class AspNetCoreGoogleLoggingContext
    {
        public LogName LogName { get; }

        public MonitoredResource Resource { get; }

        public string? ServiceVersion { get; }

        public CategoryHandling CategoryHandling { get; }

        public EventIdHandling EventIdHandling { get; }

        public AspNetCoreGoogleLoggingContext(
            LogName logName,
            MonitoredResource resource,
            string? serviceVersion,
            CategoryHandling categoryHandling,
            EventIdHandling eventIdHandling)
        {
            LogName = logName ?? throw new ArgumentNullException(nameof(logName));
            Resource = resource ?? throw new ArgumentNullException(nameof(resource));
            ServiceVersion = serviceVersion;
            CategoryHandling = categoryHandling;
            EventIdHandling = eventIdHandling;
        }
    }
}
using System;
using Google.Api;
using Google.Cloud.Logging.V2;

namespace NCoreUtils.Logging.Google
{
    public class GoogleLoggingContext
    {
        public LogName LogName { get; }

        public MonitoredResource Resource { get; }

        public GoogleLoggingContext(LogName logName, MonitoredResource resource)
        {
            LogName = logName ?? throw new ArgumentNullException(nameof(logName));
            Resource = resource ?? throw new ArgumentNullException(nameof(resource));
        }
    }
}
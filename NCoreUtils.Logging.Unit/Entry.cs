using System;
using System.Collections.Immutable;

namespace NCoreUtils.Logging.Unit
{
    public class Entry
    {
        public ImmutableDictionary<string, string> Labels { get; }

        public DateTimeOffset Timestamp { get; }

        public string Message { get; }

        public string? Method { get; }

        public string? Url { get; }

        public string? UserAgent { get; }

        public string? Referrer { get; }

        public string? RemoteIp { get; }

        public string? User { get; }

        public Entry(
            ImmutableDictionary<string, string> labels,
            DateTimeOffset timestamp,
            string message,
            string? method,
            string? url,
            string? userAgent,
            string? referrer,
            string? remoteIp,
            string? user)
        {
            Labels = labels;
            Timestamp = timestamp;
            Message = message;
            Method = method;
            Url = url;
            UserAgent = userAgent;
            Referrer = referrer;
            RemoteIp = remoteIp;
            User = user;
        }
    }
}
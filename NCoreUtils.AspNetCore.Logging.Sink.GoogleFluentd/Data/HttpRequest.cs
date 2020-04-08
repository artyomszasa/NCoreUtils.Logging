using System;
using System.Text.Json.Serialization;

namespace NCoreUtils.Logging.Google.Data
{
    public class HttpRequest
    {
        public string RequestMethod { get; }

        public string RequestUrl { get; }

        public int Status { get; }

        public long? ResponseSize { get; }

        public string UserAgent { get; }

        public string RemoteIp { get; }

        public string Referer { get; }

        [JsonConverter(typeof(LatencyConveter))]
        public TimeSpan? Latency { get; }

        public HttpRequest(string requestMethod, string requestUrl, int status, long? responseSize, string userAgent, string remoteIp, string referer, TimeSpan? latency)
        {
            RequestMethod = requestMethod;
            RequestUrl = requestUrl;
            Status = status;
            ResponseSize = responseSize;
            UserAgent = userAgent;
            RemoteIp = remoteIp;
            Referer = referer;
            Latency = latency;
        }
    }
}
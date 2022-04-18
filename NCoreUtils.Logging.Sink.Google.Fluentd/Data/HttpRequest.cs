using System;
using System.Text.Json.Serialization;

namespace NCoreUtils.Logging.Google.Data
{
    public class HttpRequest
    {
        [JsonPropertyName("requestMethod")]
        public string RequestMethod { get; private set; }

        [JsonPropertyName("requestUrl")]
        public string RequestUrl { get; private set; }

        [JsonPropertyName("status")]
        public int Status { get; private set; }

        [JsonPropertyName("responseSize")]
        public long? ResponseSize { get; private set; }

        [JsonPropertyName("userAgent")]
        public string UserAgent { get; private set; }

        [JsonPropertyName("remoteIp")]
        public string RemoteIp { get; private set; }

        [JsonPropertyName("referer")]
        public string Referer { get; private set; }

        [JsonConverter(typeof(LatencyConveter))]
        [JsonPropertyName("latency")]
        public TimeSpan? Latency { get; private set; }

        [JsonConstructor]
        public HttpRequest(
            string requestMethod,
            string requestUrl,
            int status,
            long? responseSize,
            string userAgent,
            string remoteIp,
            string referer,
            TimeSpan? latency)
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

        public HttpRequest Update(
            string requestMethod,
            string requestUrl,
            int status,
            long? responseSize,
            string userAgent,
            string remoteIp,
            string referer,
            TimeSpan? latency)
        {
            RequestMethod = requestMethod;
            RequestUrl = requestUrl;
            Status = status;
            ResponseSize = responseSize;
            UserAgent = userAgent;
            RemoteIp = remoteIp;
            Referer = referer;
            Latency = latency;
            return this;
        }
    }
}
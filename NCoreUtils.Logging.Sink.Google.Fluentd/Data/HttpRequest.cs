using System;
using System.Text.Json.Serialization;

namespace NCoreUtils.Logging.Google.Data
{
    public class HttpRequest
    {
        [JsonPropertyName("requestMethod")]
        public string RequestMethod { get; }

        [JsonPropertyName("requestUrl")]
        public string RequestUrl { get; }

        [JsonPropertyName("status")]
        public int Status { get; }

        [JsonPropertyName("responseSize")]
        public long? ResponseSize { get; }

        [JsonPropertyName("userAgent")]
        public string UserAgent { get; }

        [JsonPropertyName("remoteIp")]
        public string RemoteIp { get; }

        [JsonPropertyName("referer")]
        public string Referer { get; }

        [JsonConverter(typeof(LatencyConveter))]
        [JsonPropertyName("latency")]
        public TimeSpan? Latency { get; }

        [JsonConstructor]
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
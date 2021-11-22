using System.Text.Json.Serialization;

namespace NCoreUtils.Logging.Google.Data
{
    public class ErrorContext
    {
        [JsonPropertyName("method")]
        public string Method { get; }

        [JsonPropertyName("url")]
        public string Url { get; }

        [JsonPropertyName("userAgent")]
        public string UserAgent { get; }

        [JsonPropertyName("referer")]
        public string Referer { get; }

        [JsonPropertyName("responseStatusCode")]
        public int ResponseStatusCode { get; }

        [JsonPropertyName("remoteIp")]
        public string RemoteIp { get; }

        [JsonPropertyName("user")]
        public string User { get; }

        [JsonConstructor]
        public ErrorContext(string method, string url, string userAgent, string referer, int responseStatusCode, string remoteIp, string user)
        {
            Method = method;
            Url = url;
            UserAgent = userAgent;
            Referer = referer;
            ResponseStatusCode = responseStatusCode;
            RemoteIp = remoteIp;
            User = user;
        }
    }
}
using System.Text.Json.Serialization;

namespace NCoreUtils.Logging.Google.Data
{
    public class ErrorContext
    {
        [JsonPropertyName("method")]
        public string Method { get; private set; }

        [JsonPropertyName("url")]
        public string Url { get; private set; }

        [JsonPropertyName("userAgent")]
        public string UserAgent { get; private set; }

        [JsonPropertyName("referer")]
        public string Referer { get; private set; }

        [JsonPropertyName("responseStatusCode")]
        public int ResponseStatusCode { get; private set; }

        [JsonPropertyName("remoteIp")]
        public string RemoteIp { get; private set; }

        [JsonPropertyName("user")]
        public string User { get; private set; }

        [JsonConstructor]
        public ErrorContext(
            string method,
            string url,
            string userAgent,
            string referer,
            int responseStatusCode,
            string remoteIp,
            string user)
        {
            Method = method;
            Url = url;
            UserAgent = userAgent;
            Referer = referer;
            ResponseStatusCode = responseStatusCode;
            RemoteIp = remoteIp;
            User = user;
        }

        public ErrorContext Update(
            string method,
            string url,
            string userAgent,
            string referer,
            int responseStatusCode,
            string remoteIp,
            string user)
        {
            Method = method;
            Url = url;
            UserAgent = userAgent;
            Referer = referer;
            ResponseStatusCode = responseStatusCode;
            RemoteIp = remoteIp;
            User = user;
            return this;
        }
    }
}
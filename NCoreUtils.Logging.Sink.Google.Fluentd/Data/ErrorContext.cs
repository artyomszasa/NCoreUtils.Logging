using System.Text.Json.Serialization;

namespace NCoreUtils.Logging.Google.Data;

[method: JsonConstructor]
public class ErrorContext(
    string method,
    string url,
    string userAgent,
    string referer,
    int responseStatusCode,
    string remoteIp,
    string user)
{
    [JsonPropertyName("method")]
    public string Method { get; private set; } = method;

    [JsonPropertyName("url")]
    public string Url { get; private set; } = url;

    [JsonPropertyName("userAgent")]
    public string UserAgent { get; private set; } = userAgent;

    [JsonPropertyName("referer")]
    public string Referer { get; private set; } = referer;

    [JsonPropertyName("responseStatusCode")]
    public int ResponseStatusCode { get; private set; } = responseStatusCode;

    [JsonPropertyName("remoteIp")]
    public string RemoteIp { get; private set; } = remoteIp;

    [JsonPropertyName("user")]
    public string User { get; private set; } = user;

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
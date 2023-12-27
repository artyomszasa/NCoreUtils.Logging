using System;
using System.Text.Json.Serialization;

namespace NCoreUtils.Logging.Google.Data;

[method: JsonConstructor]
public class HttpRequest(
    string requestMethod,
    string requestUrl,
    int status,
    long? responseSize,
    string userAgent,
    string remoteIp,
    string referer,
    TimeSpan? latency)
{
    [JsonPropertyName("requestMethod")]
    public string RequestMethod { get; private set; } = requestMethod;

    [JsonPropertyName("requestUrl")]
    public string RequestUrl { get; private set; } = requestUrl;

    [JsonPropertyName("status")]
    public int Status { get; private set; } = status;

    [JsonPropertyName("responseSize")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public long? ResponseSize { get; private set; } = responseSize;

    [JsonPropertyName("userAgent")]
    public string UserAgent { get; private set; } = userAgent;

    [JsonPropertyName("remoteIp")]
    public string RemoteIp { get; private set; } = remoteIp;

    [JsonPropertyName("referer")]
    public string Referer { get; private set; } = referer;

    [JsonConverter(typeof(LatencyConveter))]
    [JsonPropertyName("latency")]
    public TimeSpan? Latency { get; private set; } = latency;

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